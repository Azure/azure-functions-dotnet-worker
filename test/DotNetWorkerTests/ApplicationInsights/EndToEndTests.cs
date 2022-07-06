﻿using System.Collections.Immutable;
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

public class EndToEndTests
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
                    .AddApplicationInsights(appInsightsOptions => appInsightsOptions.InstrumentationKey = "abc")
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
    public async Task Logger_SendsTraceTelemetry()
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

        // Log written in test function should go to App Insights directly
        Assert.Collection(_channel.Telemetries,
            t =>
            {
                var dependency = (DependencyTelemetry)t;

                Assert.Equal("TestName", dependency.Context.Operation.Name);
                Assert.Equal(invocation.TraceContext.TraceParent, dependency.Context.Operation.Id);

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

                ValidateProperties(trace);
            });
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

        public void TestFunction(FunctionContext context)
        {
            var logger = context.GetLogger("TestFunction");
            logger.LogWarning("Test");
        }
    }
}
