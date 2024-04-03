// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Tests;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Azure.Functions.Worker.Diagnostics;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Azure.Functions.Worker.Tests.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Xunit;

namespace DotNetWorker.OpenTelemetry.Tests;

public class EndToEndTests
{
    private IFunctionsApplication _application;
    private IInvocationFeaturesFactory _invocationFeaturesFactory;
    private readonly OtelFunctionDefinition _funcDefinition = new();

    private IHost InitializeHost()
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
        using var host = InitializeHost();
        var context = CreateContext(host);
        await _application.InvokeFunctionAsync(context);
        var activity = OtelFunctionDefinition.LastActivity;

        Assert.Equal(activity.Id, context.TraceContext.TraceParent);
        Assert.Equal("InvokeFunctionAsync", activity.OperationName);
    }

    [Fact]
    public void ResourceDetectorLocalDevelopment()
    {
        FunctionsResourceDetector detector = new FunctionsResourceDetector();
        Resource resource = detector.Detect();

        Assert.Equal(3, resource.Attributes.Count());
        var attribute = resource.Attributes.FirstOrDefault(a => a.Key == "cloud.provider");
        Assert.Equal("azure", resource.Attributes.FirstOrDefault(a => a.Key == "cloud.provider").Value);
        Assert.Equal("azure_functions", resource.Attributes.FirstOrDefault(a => a.Key == "cloud.platform").Value);
    }

    [Fact]
    public void ResourceDetectorLocalDevelopment2()
    {
        using var _ = SetupDefaultEnvironmentVariables();

        FunctionsResourceDetector detector = new FunctionsResourceDetector();
        Resource resource = detector.Detect();

        Assert.Equal($"/subscriptions/AAAAA-AAAAA-AAAAA-AAA/resourceGroups/rg/providers/Microsoft.Web/sites/appName"
            , resource.Attributes.FirstOrDefault(a => a.Key == "cloud.resource.id").Value);
        Assert.Equal($"EastUS", resource.Attributes.FirstOrDefault(a => a.Key == "cloud.region").Value);
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

        public void TestFunction(FunctionContext context)
        {
            LastActivity = Activity.Current;
            Activity.Current.ActivityTraceFlags = ActivityTraceFlags.Recorded;
            Activity.Current.AddTag("CustomKey", "CustomValue");

            var logger = context.GetLogger("TestFunction");
            logger.LogWarning("Test");

            HttpClient httpClient = new HttpClient();
            httpClient.GetAsync("https://www.bing.com").GetAwaiter().GetResult();

            if (context.Items.ContainsKey("_throw"))
            {
                throw new InvalidOperationException("boom!");
            }
        }
    }
}
