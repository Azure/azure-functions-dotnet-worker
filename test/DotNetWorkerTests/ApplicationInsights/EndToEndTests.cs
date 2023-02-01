using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Azure.Functions.Worker.Diagnostics;
using Microsoft.Azure.Functions.Worker.Tests.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests.ApplicationInsights;

public class EndToEndTests : IDisposable
{
    private readonly TestTelemetryChannel _channel;
    private readonly IHost _host;
    private readonly IFunctionsApplication _application;
    private readonly IInvocationFeaturesFactory _invocationFeaturesFactory;
    private readonly AppInsightsFunctionDefinition _funcDefinition = new();

    public EndToEndTests()
    {
        _channel = new TestTelemetryChannel();

        _host = new HostBuilder()
            .ConfigureServices(services =>
            {
                var functionsBuilder = services.AddFunctionsWorkerCore();
                functionsBuilder
                    .AddApplicationInsights(appInsightsOptions =>
                    {
#pragma warning disable CS0618 // Obsolete member. Test case, this is fine to use.
                        appInsightsOptions.InstrumentationKey = "abc";
#pragma warning restore CS0618 // Obsolete member. Test case, this is fine to use.

                        // keep things more deterministic for tests
                        appInsightsOptions.EnableAdaptiveSampling = false;
                        appInsightsOptions.EnableDependencyTrackingTelemetryModule = false;
                    })
                    .AddApplicationInsightsLogger();

                functionsBuilder.UseDefaultWorkerMiddleware();
                services.AddDefaultInputConvertersToWorkerOptions();

                // Register our own in-memory channel
                services.AddSingleton<ITelemetryChannel>(_channel);
                services.AddSingleton(_ => new Mock<IWorkerDiagnostics>().Object);
            })
            .Build();

        _application = _host.Services.GetService<IFunctionsApplication>();
        _invocationFeaturesFactory = _host.Services.GetService<IInvocationFeaturesFactory>();

        _application.LoadFunction(_funcDefinition);
    }

    private FunctionContext CreateContext()
    {
        var invocation = new TestFunctionInvocation(functionId: _funcDefinition.Id);

        var features = _invocationFeaturesFactory.Create();
        features.Set<FunctionInvocation>(invocation);
        var inputConversionProvider = _host.Services.GetRequiredService<IInputConversionFeatureProvider>();
        inputConversionProvider.TryCreate(typeof(DefaultInputConversionFeature), out var inputConversion);
        features.Set<IFunctionBindingsFeature>(new TestFunctionBindingsFeature());
        features.Set<IInputConversionFeature>(inputConversion);

        return _application.CreateContext(features);
    }

    [Fact]
    public async Task Logger_SendsTraceAndDependencyTelemetry()
    {
        var context = CreateContext();
        var invocationId = context.InvocationId;

        await _application.InvokeFunctionAsync(context);

        var activity = AppInsightsFunctionDefinition.LastActivity;

        IEnumerable<ITelemetry> telemetries = await WaitForTelemetries(expectedCount: 2);

        Assert.Collection(telemetries,
            t => ValidateDependencyTelemetry((DependencyTelemetry)t, context, activity),
            t => ValidateTraceTelemetry((TraceTelemetry)t, context, activity));
    }

    [Fact]
    public async Task Logger_Exception_SendsTraceAndExceptionAndDependencyTelemetry()
    {
        var context = CreateContext();
        context.Items["_throw"] = true;
        var invocationId = context.InvocationId;

        await Assert.ThrowsAsync<InvalidOperationException>(() => _application.InvokeFunctionAsync(context));

        var activity = AppInsightsFunctionDefinition.LastActivity;

        IEnumerable<ITelemetry> telemetries = await WaitForTelemetries(expectedCount: 3);

        Assert.Collection(telemetries,
            t => ValidateDependencyTelemetry((DependencyTelemetry)t, context, activity),
            t => ValidateExceptionTelemetry((ExceptionTelemetry)t, context, activity),
            t => ValidateTraceTelemetry((TraceTelemetry)t, context, activity));
    }

    private async Task<IEnumerable<ITelemetry>> WaitForTelemetries(int expectedCount)
    {
        IEnumerable<ITelemetry> telemetries = null;

        await Functions.Tests.TestUtility.RetryAsync(() =>
        {
            // App Insights can potentially log this, which causes tests to be flaky. Explicitly ignore.
            var aiTelemetry = _channel.Telemetries.Where(p => p is TraceTelemetry t && t.Message.Contains("AI: TelemetryChannel found a telemetry item"));
            telemetries = _channel.Telemetries.Except(aiTelemetry);
            return Task.FromResult(telemetries.Count() >= expectedCount);
        }, timeout: 5000, pollingInterval: 50, userMessageCallback: () => $"Expected {expectedCount} telemetries. Found [{string.Join(", ", telemetries.Select(t => t.GetType().Name))}].");

        return telemetries;
    }

    private static void ValidateDependencyTelemetry(DependencyTelemetry dependency, FunctionContext context, Activity activity)
    {
        Assert.Equal("CustomValue", dependency.Properties["CustomKey"]);

        Assert.Equal(TraceConstants.FunctionsInvokeActivityName, dependency.Name);
        Assert.Equal(activity.RootId, dependency.Context.Operation.Id);

        Assert.Equal(context.InvocationId, dependency.Properties[TraceConstants.AttributeFaasExecution]);
        Assert.Contains(TraceConstants.AttributeSchemaUrl, dependency.Properties.Keys);
    }

    private static void ValidateTraceTelemetry(TraceTelemetry trace, FunctionContext context, Activity activity)
    {
        Assert.Equal("Test", trace.Message);
        Assert.Equal(SeverityLevel.Warning, trace.SeverityLevel);

        // Check that scopes show up by default                
        Assert.Equal("TestName", trace.Properties[FunctionInvocationScope.FunctionNameKey]);
        Assert.Equal(context.InvocationId, trace.Properties[FunctionInvocationScope.FunctionInvocationIdKey]);

        Assert.Equal(activity.RootId, trace.Context.Operation.Id);
    }

    private static void ValidateExceptionTelemetry(ExceptionTelemetry exception, FunctionContext context, Activity activity)
    {
        Assert.Equal("boom!", exception.Message);
        var edi = exception.ExceptionDetailsInfoList.Single();

        // stack trace is stored internally and not available to verify
        Assert.Contains(nameof(InvalidOperationException), edi.TypeName);
        Assert.Contains("boom!", edi.Message);

        Assert.Equal(activity.RootId, exception.Context.Operation.Id);
    }
    public void Dispose()
    {
        _host?.Dispose();
    }

    internal class AppInsightsFunctionDefinition : FunctionDefinition
    {
        public static readonly string DefaultPathToAssembly = typeof(AppInsightsFunctionDefinition).Assembly.Location;
        public static readonly string DefaultEntryPoint = $"{typeof(AppInsightsFunctionDefinition).FullName}.{nameof(TestFunction)}";
        public static readonly string DefaultId = "TestId";
        public static readonly string DefaultName = "TestName";

        public AppInsightsFunctionDefinition()
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
            Activity.Current.AddTag("CustomKey", "CustomValue");

            var logger = context.GetLogger("TestFunction");
            logger.LogWarning("Test");

            if (context.Items.ContainsKey("_throw"))
            {
                throw new InvalidOperationException("boom!");
            }
        }
    }
}
