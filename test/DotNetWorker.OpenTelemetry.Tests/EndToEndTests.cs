// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Tests;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Azure.Functions.Worker.Diagnostics;
using Microsoft.Azure.Functions.Worker.Tests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Xunit;

using CoreTraceConstants = Microsoft.Azure.Functions.Worker.Diagnostics.TraceConstants;

namespace Microsoft.Azure.Functions.Worker.OpenTelemetry.Tests;

public class EndToEndTests
{
    private IFunctionsApplication _application;
    private IInvocationFeaturesFactory _invocationFeaturesFactory;
    private readonly OtelFunctionDefinition _funcDefinition = new();

    private IHost InitializeHost(string schemaVersion = null)
    {
        var host = new HostBuilder()
           .ConfigureServices(services =>
           {
               var functionsBuilder = services.AddFunctionsWorkerCore();
               functionsBuilder.UseDefaultWorkerMiddleware();
               services.AddDefaultInputConvertersToWorkerOptions();
               services.AddOpenTelemetry()
                    .UseFunctionsWorkerDefaults();

               services.AddSingleton(_ => new Mock<IWorkerDiagnostics>().Object);
           })
           .ConfigureFunctionsWorkerDefaults((WorkerOptions options) =>
           {
               if (schemaVersion is not null)
               {
                   options.Capabilities["WorkerOpenTelemetrySchemaVersion"] = schemaVersion;
               }
           })
           .Build();

        _application = host.Services.GetService<IFunctionsApplication>();
        _invocationFeaturesFactory = host.Services.GetService<IInvocationFeaturesFactory>();

        _application.LoadFunction(_funcDefinition);

        return host;
    }

    private FunctionContext CreateContext(IHost host)
    {
        var invocation = new TestFunctionInvocation(functionId: _funcDefinition.Id);

        var features = _invocationFeaturesFactory.Create();
        features.Set<FunctionInvocation>(invocation);
        var inputConversionProvider = host.Services.GetRequiredService<IInputConversionFeatureProvider>();
        inputConversionProvider.TryCreate(typeof(DefaultInputConversionFeature), out var inputConversion);
        features.Set<IFunctionBindingsFeature>(new TestFunctionBindingsFeature());
        features.Set<IInputConversionFeature>(inputConversion);

        return _application.CreateContext(features);
    }

    [Fact]
    public async Task ContextPropagation()
    {
        using var testListener = new ActivityTestListener("Microsoft.Azure.Functions.Worker");
        using var host = InitializeHost();
        var context = CreateContext(host);
        await _application.InvokeFunctionAsync(context);
        var activity = OtelFunctionDefinition.LastActivity;

        if (ActivityContext.TryParse(context.TraceContext.TraceParent, context.TraceContext.TraceState, true, out ActivityContext activityContext))
        {
            Assert.Equal("function TestName", activity.OperationName);
            Assert.Equal(activity.TraceId, activityContext.TraceId);
            Assert.Equal(activity.TraceStateString, activityContext.TraceState);
            Assert.Equal(ActivityKind.Internal, activity.Kind);
            Assert.Contains(activity.Tags, t => t.Key == CoreTraceConstants.OTelAttributes_1_37_0.InvocationId && t.Value == context.InvocationId);
        }
        else
        {
            Assert.Fail("Failed to parse ActivityContext");
        }   
    }

    [Fact]
    public async Task ContextPropagationV17()
    {
        using var testListener = new ActivityTestListener("Microsoft.Azure.Functions.Worker");
        using var host = InitializeHost("1.17.0");
        var context = CreateContext(host);
        await _application.InvokeFunctionAsync(context);
        var activity = OtelFunctionDefinition.LastActivity;

        if (ActivityContext.TryParse(context.TraceContext.TraceParent, context.TraceContext.TraceState, true, out ActivityContext activityContext))
        {
            Assert.Equal("Invoke", activity.OperationName);
            Assert.Equal(activity.TraceId, activityContext.TraceId);
            Assert.Equal(activity.TraceStateString, activityContext.TraceState);
            Assert.Equal(ActivityKind.Server, activity.Kind);
            Assert.Contains(activity.Tags, t => t.Key == CoreTraceConstants.OTelAttributes_1_17_0.InvocationId && t.Value == context.InvocationId);
        }
        else
        {
            Assert.Fail("Failed to parse ActivityContext");
        }
    }

    [Fact]
    public async Task ContextPropagation_InvalidVersion()
    {
        try
        {
            using var host = InitializeHost("0.0.0");
            var context = CreateContext(host);
            await _application.InvokeFunctionAsync(context);
        }
        catch (Exception ex)
        {
            Assert.IsType<ArgumentException>(ex);
        }
    }

    [Fact]
    public async Task ContextPropagation_EmptyVersion()
    {
        using var host = InitializeHost(string.Empty);
        var context = CreateContext(host);
        await _application.InvokeFunctionAsync(context);

        var activity = OtelFunctionDefinition.LastActivity;
        Assert.Null(activity);
    }

    [Fact]
    public async Task ContextPropagationWithTriggerInstrumentation()
    {
        using var host = InitializeHost();
        var context = CreateContext(host);
        using Activity testActivity = new Activity("ASPNetCoreMockActivity");
        testActivity.Start();
        await _application.InvokeFunctionAsync(context);
        var activity = OtelFunctionDefinition.LastActivity;
               
        Assert.Equal(activity.Id, testActivity.Id);
        Assert.Equal(activity.OperationName, testActivity.OperationName);
        Assert.Equal(activity.SpanId, testActivity.SpanId);
        Assert.Equal(activity.TraceId, testActivity.TraceId);
        Assert.Equal(activity.ActivityTraceFlags, testActivity.ActivityTraceFlags);
        Assert.Equal(activity.TraceStateString, testActivity.TraceStateString);
    }

    [Fact]
    public void ResourceDetectorLocalDevelopment()
    {
        FunctionsResourceDetector detector = new FunctionsResourceDetector();
        Resource resource = detector.Detect();

        Assert.Equal(4, resource.Attributes.Count());
        Assert.Equal("testhost", resource.Attributes.FirstOrDefault(a => a.Key == "service.name").Value);
        Assert.Contains("dotnetiso", resource.Attributes.FirstOrDefault(a => a.Key == "ai.sdk.prefix").Value as string);
        Assert.DoesNotContain(resource.Attributes, a => a.Key == "deployment.environment.name");
    }

    [Fact]
    public void ResourceDetector()
    {
        using var _ = SetupDefaultEnvironmentVariables();
        FunctionsResourceDetector detector = new FunctionsResourceDetector();
        Resource resource = detector.Detect();

        Assert.Equal($"/subscriptions/AAAAA-AAAAA-AAAAA-AAA/resourceGroups/rg/providers/Microsoft.Web/sites/appName"
            , resource.Attributes.FirstOrDefault(a => a.Key == "cloud.resource_id").Value);
        Assert.Equal($"EastUS", resource.Attributes.FirstOrDefault(a => a.Key == "cloud.region").Value);
        Assert.Equal("appName", resource.Attributes.FirstOrDefault(a => a.Key == "service.name").Value);
    }

    [Fact]
    public void ResourceDetector_SiteUpdateId_AddedWhenInAzure()
    {
        using var _ = new TestScopedEnvironmentVariable(new Dictionary<string, string>
        {
            { "WEBSITE_SITE_NAME", "appName" },
            { "WEBSITE_RESOURCE_GROUP", "rg" },
            { "WEBSITE_OWNER_NAME", "AAAAA-AAAAA-AAAAA-AAA+appName-EastUSwebspace" },
            { "REGION_NAME", "EastUS" },
            { "FUNCTIONS_SITE_UPDATE_ID", "update-123" }
        });

        FunctionsResourceDetector detector = new FunctionsResourceDetector();
        Resource resource = detector.Detect();

        Assert.Equal("update-123", resource.Attributes.FirstOrDefault(a => a.Key == "azure.functions.site.update_id").Value);
    }

    [Fact]
    public void ResourceDetector_SlotName_AddedAsDeploymentEnvironmentName()
    {
        using var _ = new TestScopedEnvironmentVariable(new Dictionary<string, string>
        {
            { "WEBSITE_SITE_NAME", "appName" },
            { "WEBSITE_SLOT_NAME", "staging" }
        });

        FunctionsResourceDetector detector = new FunctionsResourceDetector();
        Resource resource = detector.Detect();

        Assert.Equal("staging", resource.Attributes.FirstOrDefault(a => a.Key == "deployment.environment.name").Value);
    }
    
    [Fact]
    public void ResourceDetector_SlotName_NotAddedWhenLocal()
    {
        using var _ = new TestScopedEnvironmentVariable(new Dictionary<string, string>
        {
            { "WEBSITE_SLOT_NAME", "staging" }
        });

        FunctionsResourceDetector detector = new FunctionsResourceDetector();
        Resource resource = detector.Detect();

        Assert.DoesNotContain(resource.Attributes, a => a.Key == "deployment.environment.name");
    }

    [Fact]
    public void ResourceDetector_SiteUpdateId_NotAddedWhenLocal()
    {
        using var _ = new TestScopedEnvironmentVariable(new Dictionary<string, string>
        {
            { "FUNCTIONS_SITE_UPDATE_ID", "update-123" }
        });

        FunctionsResourceDetector detector = new FunctionsResourceDetector();
        Resource resource = detector.Detect();

        Assert.DoesNotContain(resource.Attributes, a => a.Key == "azure.functions.site.update_id");
    }

    public static IEnumerable<object[]> ServiceNameSkippedData =>
    [
        // OTEL_SERVICE_NAME in Azure
        [new Dictionary<string, string> { { "WEBSITE_SITE_NAME", "appName" }, { "OTEL_SERVICE_NAME", "my-custom-service" } }],
        // OTEL_SERVICE_NAME locally
        [new Dictionary<string, string> { { "OTEL_SERVICE_NAME", "my-custom-service" } }],
        // OTEL_RESOURCE_ATTRIBUTES[service.name] in Azure
        [new Dictionary<string, string> { { "WEBSITE_SITE_NAME", "appName" }, { "OTEL_RESOURCE_ATTRIBUTES", "service.name=my-custom-service,other.key=value" } }],
        // OTEL_RESOURCE_ATTRIBUTES[service.name] locally
        [new Dictionary<string, string> { { "OTEL_RESOURCE_ATTRIBUTES", "service.name=my-custom-service" } }],
        // OTEL_SERVICE_NAME takes priority over OTEL_RESOURCE_ATTRIBUTES
        [new Dictionary<string, string> { { "WEBSITE_SITE_NAME", "appName" }, { "OTEL_SERVICE_NAME", "from-env" }, { "OTEL_RESOURCE_ATTRIBUTES", "service.name=from-attributes" } }],
        // Leading/trailing whitespace around pair is trimmed
        [new Dictionary<string, string> { { "WEBSITE_SITE_NAME", "appName" }, { "OTEL_RESOURCE_ATTRIBUTES", "other.key=value, service.name=my-custom-service" } }],
    ];

    [Theory]
    [MemberData(nameof(ServiceNameSkippedData))]
    public void ResourceDetector_ServiceName_SkippedWhenConfigured(Dictionary<string, string> envVars)
    {
        using var _ = new TestScopedEnvironmentVariable(envVars);
        FunctionsResourceDetector detector = new FunctionsResourceDetector();
        Resource resource = detector.Detect();

        Assert.DoesNotContain(resource.Attributes, a => a.Key == "service.name");
    }

    [Fact]
    public void ResourceDetector_OtelResourceAttributes_KeyMatching_CaseSensitive()
    {
        using var _ = new TestScopedEnvironmentVariable(new Dictionary<string, string>
        {
            { "WEBSITE_SITE_NAME", "appName" },
            { "OTEL_RESOURCE_ATTRIBUTES", "Service.Name=my-custom-service" }
        });

        // Keys are case-sensitive; "Service.Name" should NOT match "service.name"
        FunctionsResourceDetector detector = new FunctionsResourceDetector();
        Resource resource = detector.Detect();

        Assert.Equal("appName", resource.Attributes.FirstOrDefault(a => a.Key == "service.name").Value);
    }

    [Theory]
    [InlineData(false)] // locally
    [InlineData(true)]  // in Azure
    public void ResourceDetector_OtelResourceAttributes_ServiceVersion_SkipsServiceVersion(bool inAzure)
    {
        var envVars = new Dictionary<string, string> { { "OTEL_RESOURCE_ATTRIBUTES", "service.version=1.2.3" } };
        if (inAzure)
        {
            envVars["WEBSITE_SITE_NAME"] = "appName";
        }

        using var _ = new TestScopedEnvironmentVariable(envVars);
        FunctionsResourceDetector detector = new FunctionsResourceDetector();
        Resource resource = detector.Detect();

        Assert.DoesNotContain(resource.Attributes, a => a.Key == "service.version");
    }

    [Fact]
    public void ResourceDetector_CloudAttributes_AddedWhenInAzure()
    {
        using var _ = new TestScopedEnvironmentVariable("WEBSITE_SITE_NAME", "appName");

        FunctionsResourceDetector detector = new FunctionsResourceDetector();
        Resource resource = detector.Detect();

        Assert.Equal("azure", resource.Attributes.FirstOrDefault(a => a.Key == "cloud.provider").Value);
        Assert.Equal("azure_functions", resource.Attributes.FirstOrDefault(a => a.Key == "cloud.platform").Value);
    }

    [Fact]
    public void ResourceDetector_CloudAttributes_NotAddedWhenLocal()
    {
        FunctionsResourceDetector detector = new FunctionsResourceDetector();
        Resource resource = detector.Detect();

        Assert.DoesNotContain(resource.Attributes, a => a.Key == "cloud.provider");
        Assert.DoesNotContain(resource.Attributes, a => a.Key == "cloud.platform");
        Assert.DoesNotContain(resource.Attributes, a => a.Key == "cloud.region");
        Assert.DoesNotContain(resource.Attributes, a => a.Key == "cloud.resource_id");
    }

    [Theory]
    [InlineData("WEBSITE_OWNER_NAME", "AAAAA-AAAAA-AAAAA-AAA+appName-EastUSwebspace")] // missing resource group
    [InlineData("WEBSITE_RESOURCE_GROUP", "rg")]                                        // missing owner name
    public void ResourceDetector_CloudResourceId_NotAddedWhenEnvVarMissing(string presentEnvVar, string value)
    {
        using var _ = new TestScopedEnvironmentVariable(new Dictionary<string, string>
        {
            { "WEBSITE_SITE_NAME", "appName" },
            { presentEnvVar, value }
        });

        FunctionsResourceDetector detector = new FunctionsResourceDetector();
        Resource resource = detector.Detect();

        Assert.DoesNotContain(resource.Attributes, a => a.Key == "cloud.resource_id");
    }

    [Fact]
    public void ResourceDetector_ProcessId_IsInt()
    {
        FunctionsResourceDetector detector = new FunctionsResourceDetector();
        Resource resource = detector.Detect();

        var processId = resource.Attributes.FirstOrDefault(a => a.Key == "process.pid").Value;
        Assert.IsType<long>(processId);
        Assert.Equal(Process.GetCurrentProcess().Id, (int)(long)processId);
    }

    [Fact]
    public void ResourceDetector_OtelResourceAttributes_EmptyValue_TreatedAsConfigured()
    {
        // A key with an empty value (service.name=) is still treated as explicitly configured —
        // the OTel SDK will set service.name to empty from OTEL_RESOURCE_ATTRIBUTES.
        using var _ = new TestScopedEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES", "service.name=");

        FunctionsResourceDetector detector = new FunctionsResourceDetector();
        Resource resource = detector.Detect();

        Assert.DoesNotContain(resource.Attributes, a => a.Key == "service.name");
    }

    [Fact]
    public void ResourceDetector_OtelResourceAttributes_WhitespaceOnly_NotTreatedAsConfigured()
    {
        // A whitespace-only value with 3+ spaces passes the Length > 2 guard but Trim() + IndexOf('=')
        // correctly returns false — no '=' found means the key is not matched.
        using var _ = new TestScopedEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES", "   ");

        FunctionsResourceDetector detector = new FunctionsResourceDetector();
        Resource resource = detector.Detect();

        Assert.Contains(resource.Attributes, a => a.Key == "service.name");
    }

    private static IDisposable SetupDefaultEnvironmentVariables()
    {
        return new TestScopedEnvironmentVariable(new Dictionary<string, string>
        {
            { "WEBSITE_SITE_NAME", "appName" },
            { "WEBSITE_RESOURCE_GROUP", "rg" },
            { "WEBSITE_OWNER_NAME", "AAAAA-AAAAA-AAAAA-AAA+appName-EastUSwebspace" },
            { "REGION_NAME", "EastUS" }
        });
    }

    internal class OtelFunctionDefinition : FunctionDefinition
    {
        public static readonly string DefaultPathToAssembly = typeof(OtelFunctionDefinition).Assembly.Location;
        public static readonly string DefaultEntryPoint = $"{typeof(OtelFunctionDefinition).FullName}.{nameof(TestFunction)}";
        public static readonly string DefaultId = "TestId";
        public static readonly string DefaultName = "TestName";

        public OtelFunctionDefinition()
        {
            Parameters = (new[] { new FunctionParameter("context", typeof(FunctionContext)) }).ToImmutableArray();
        }

        public override ImmutableArray<FunctionParameter> Parameters { get; }

        public override string PathToAssembly { get; } = DefaultPathToAssembly;

        public override string EntryPoint { get; } = DefaultEntryPoint;

        public override string Id { get; } = DefaultId;

        public override string Name { get; } = DefaultName;

        public override IImmutableDictionary<string, BindingMetadata> InputBindings { get; } = ImmutableDictionary<string, BindingMetadata>.Empty;

        public override IImmutableDictionary<string, BindingMetadata> OutputBindings { get; } = ImmutableDictionary<string, BindingMetadata>.Empty;

        public static Activity LastActivity;

        public async Task TestFunction(FunctionContext context)
        {
            LastActivity = Activity.Current;
            Activity.Current?.AddTag("CustomKey", "CustomValue");

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            var handler = new MockHttpMessageHandler(response);
            var httpClient = new HttpClient(handler);
            await httpClient.GetAsync("http://localhost:5500");
        }
    }

    public class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public MockHttpMessageHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return await Task.FromResult(_response);
        }
    }

    internal sealed class ActivityTestListener : IDisposable
    {
        public List<Activity> Activities { get; } = new List<Activity>();
        private readonly ActivityListener _listener;

        public ActivityTestListener(string sourceName)
        {
            _listener = new ActivityListener
            {
                ShouldListenTo = s => s.Name == sourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStarted = activity => Activities.Add(activity),
                ActivityStopped = _ => { }
            };

            ActivitySource.AddActivityListener(_listener);
        }

        public void Dispose()
        {
            _listener.Dispose();
        }
    }
}
