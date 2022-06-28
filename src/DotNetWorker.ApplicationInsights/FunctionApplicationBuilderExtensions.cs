using System;
using DotNetWorker.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.WindowsServer.Channel.Implementation;
using Microsoft.ApplicationInsights.WorkerService;
using Microsoft.Azure.Functions.Worker.ApplicationInsights;
using Microsoft.Azure.WebJobs.Logging.ApplicationInsights;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker
{
    public static class FunctionsWorkerApplicationBuilderExtensions
    {
        public static IFunctionsWorkerApplicationBuilder AddApplicationInsights(this IFunctionsWorkerApplicationBuilder builder, Action<ApplicationInsightsServiceOptions>? configureOptions = null)
        {
            builder.Services.AddSingleton<ISdkVersionProvider, FunctionsWorkerVersionProvider>();
            builder.Services.AddSingleton<IRoleInstanceProvider, FunctionsWorkerRoleInstanceProvider>();
            builder.Services.AddSingleton<ITelemetryInitializer, FunctionsTelemetryInitializer>();
            builder.Services.AddSingleton<ITelemetryModule, FunctionsWorkerTelemetryModule>();

            builder.Services.AddApplicationInsightsTelemetryWorkerService();

            builder.Services.AddOptions<FunctionsWorkerApplicationInsightsOptions>()
                .Configure<IConfiguration>((options, config) =>
                {
                    var appInsightsSection = ConfigurationPath.Combine("AzureFunctionsHostJson", "logging", "applicationInsights");
                    var appInsightsConfig = config.GetSection(appInsightsSection);
                    appInsightsConfig.Bind(options);
                });

            builder.Services.AddOptions<ApplicationInsightsServiceOptions>()
                    .Configure<IOptions<FunctionsWorkerApplicationInsightsOptions>, IOptions<WorkerOptions>>((options, workerAppInsightsOptions, workerOptions) =>
                    {
                        var hostJsonAppInsightsConfig = workerAppInsightsOptions.Value;
                        configureOptions?.Invoke(options);
                    });

            return builder;
        }
    }

    internal class FunctionsWorkerApplicationInsightsOptions
    {
        /// <summary>
        /// Gets or sets Application Insights instrumentation key.
        /// </summary>
        public string InstrumentationKey { get; set; }

        /// <summary>
        /// Gets or sets Application Insights connection string. If set, this will
        /// take precedence over the InstrumentationKey and overwrite it.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets sampling settings.
        /// </summary>
        public SamplingPercentageEstimatorSettings SamplingSettings { get; set; }

        /// <summary>
        /// Gets or sets excluded types for sampling.
        /// </summary>
        public string SamplingExcludedTypes { get; set; }

        /// <summary>
        /// Gets or sets included types for sampling.
        /// </summary>
        public string SamplingIncludedTypes { get; set; }

        /// <summary>
        /// Gets or sets authentication key for Live Metrics.
        /// </summary>
        public string LiveMetricsAuthenticationApiKey { get; set; }

        /// <summary>
        /// Gets or sets flag that enables Kudu performance counters collection.
        /// https://github.com/projectkudu/kudu/wiki/Perf-Counters-exposed-as-environment-variables.
        /// Enabled by default.
        /// </summary>
        public bool EnablePerformanceCountersCollection { get; set; } = true;

        /// <summary>
        /// Gets or sets the flag that enables live metrics collection.
        /// Enabled by default.
        /// </summary>
        public bool EnableLiveMetrics { get; set; } = true;

        /// <summary>
        /// Gets or sets the flag that enables dependency tracking.
        /// Enabled by default.
        /// </summary>
        public bool EnableDependencyTracking { get; set; } = true;

        /// <summary>
        /// Configuration for dependency tracking. The dependecny tracking configuration only takes effect if EnableDependencyTracking is set to true
        /// </summary>
        public DependencyTrackingOptions DependencyTrackingOptions { get; set; }
    }

    internal class DependencyTrackingOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether to disable runtime instrumentation.
        /// </summary>
        public bool DisableRuntimeInstrumentation { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to disable Http Desktop DiagnosticSource instrumentation.
        /// </summary>
        public bool DisableDiagnosticSourceInstrumentation { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to enable legacy (x-ms*) correlation headers injection.
        /// </summary>
        public bool EnableLegacyCorrelationHeadersInjection { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to enable Request-Id correlation headers injection.
        /// </summary>
        public bool EnableRequestIdHeaderInjectionInW3CMode { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to track the SQL command text in SQL
        /// dependencies.
        /// </summary>
        public bool EnableSqlCommandTextInstrumentation { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the correlation headers would be set
        /// on outgoing http requests.
        /// </summary>
        public bool SetComponentCorrelationHttpHeaders { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether telemetry would be produced for Azure
        /// SDK methods calls and requests.
        /// </summary>
        public bool EnableAzureSdkTelemetryListener { get; set; } = true;
    }
}
