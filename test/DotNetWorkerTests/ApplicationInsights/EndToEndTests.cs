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
    private readonly IInvocationFeaturesFactory _invocationFeatures;

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
        _invocationFeatures = _host.Services.GetService<IInvocationFeaturesFactory>();
    }

    [Fact]
    public async Task Logger_SendsTraceAndDependencyTelemetry()
    {
        var def = new AppInsightsFunctionDefinition();
        _application.LoadFunction(def);
        var invocation = new TestFunctionInvocation(functionId: def.Id);

        var features = _invocationFeatures.Create();
        features.Set<FunctionInvocation>(invocation);
        var inputConversionProvider = _host.Services.GetRequiredService<IInputConversionFeatureProvider>();
        inputConversionProvider.TryCreate(typeof(DefaultInputConversionFeature), out var inputConversion);
        features.Set<IFunctionBindingsFeature>(new TestFunctionBindingsFeature());
        features.Set<IInputConversionFeature>(inputConversion);

        var context = _application.CreateContext(features);

        await _application.InvokeFunctionAsync(context);

        void ValidateProperties(ISupportProperties props)
        {
            Assert.Equal(invocation.Id, props.Properties["InvocationId"]);
            Assert.Contains("ProcessId", props.Properties.Keys);
        }

        var activity = AppInsightsFunctionDefinition.LastActivity;
        IEnumerable<ITelemetry> telemetries = null;

        // There can be a race while telemetry is flushed. Explicitly wait for what we're looking for.
        await Functions.Tests.TestUtility.RetryAsync(() =>
        {
            // App Insights can potentially log this, which causes tests to be flaky. Explicitly ignore.
            var aiTelemetry = _channel.Telemetries.Where(p => p is TraceTelemetry t && t.Message.Contains("AI: TelemetryChannel found a telemetry item"));
            telemetries = _channel.Telemetries.Except(aiTelemetry);
            return Task.FromResult(telemetries.Count() == 2);
        }, timeout: 5000, pollingInterval: 500, userMessageCallback: () => $"Expected 2 telemetries. Found [{string.Join(", ", telemetries.Select(t => t.GetType().Name))}].");

        // Log written in test function should go to App Insights directly        
        Assert.Collection(telemetries,
            t =>
            {
                var dependency = (DependencyTelemetry)t;

                Assert.Equal("TestName", dependency.Context.Operation.Name);
                Assert.Equal(activity.RootId, dependency.Context.Operation.Id);

                ValidateProperties(dependency);
            },
            t =>
            {
                var trace = (TraceTelemetry)t;
                Assert.Equal("Test", trace.Message);
                Assert.Equal(SeverityLevel.Warning, trace.SeverityLevel);

                // This ensures we've disabled scopes by default
                Assert.DoesNotContain("AzureFunctions_InvocationId", trace.Properties.Keys);

                Assert.Equal("TestName", trace.Context.Operation.Name);
                Assert.Equal(activity.RootId, trace.Context.Operation.Id);

                ValidateProperties(trace);
            });
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
            var logger = context.GetLogger("TestFunction");
            logger.LogWarning("Test");
        }
    }
}
