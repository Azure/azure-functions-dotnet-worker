// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Configuration
{
    public class Program
    {
        public static void Main()
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults((IFunctionsWorkerApplicationBuilder workerApplication) =>
                {
                    // Use any of the extension methods in WorkerConfigurationExtensions.
                })
                .ConfigureCustomConfigurationSources()
                .ConfigureServicesUsingConfiguration()
                .Build();

            host.Run();
        }
    }

    internal static class HostConfigurationExtensions
    {
        /// <summary>
        /// The IFunctionsConfigurationBuilder and IFunctionsHostBuilder types from the in-process model do not exist in the
        /// isolated worker model. Use the HostBuilder's ConfigureAppConfiguration overload that takes a HostBuilderContext instead.
        /// The context exposes the hosting environment (HostBuilderContext.HostingEnvironment, which includes ContentRootPath
        /// and EnvironmentName) that GetContext() returned in the in-process model.
        /// </summary>
        public static IHostBuilder ConfigureCustomConfigurationSources(this IHostBuilder builder)
        {
            return builder.ConfigureAppConfiguration((HostBuilderContext context, IConfigurationBuilder configurationBuilder) =>
            {
                // The isolated worker does not set a base path for file based configuration sources, so set it explicitly.
                // AddJsonFile resolves relative paths against this base path.
                configurationBuilder
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: false)
                    .AddEnvironmentVariables();
            });
        }

        /// <summary>
        /// Use the ConfigureServices overload that takes a HostBuilderContext to read the IConfiguration
        /// while registering services. HostBuilderContext.Configuration contains the sources added in ConfigureAppConfiguration.
        /// </summary>
        public static IHostBuilder ConfigureServicesUsingConfiguration(this IHostBuilder builder)
        {
            return builder.ConfigureServices((HostBuilderContext context, IServiceCollection services) =>
            {
                // "SampleSetting" is an example name: it can come from local.settings.json, application settings
                // or any of the sources added above.
                string? sampleSetting = context.Configuration["SampleSetting"];

                services.AddSingleton(new SampleSettings(sampleSetting));
            });
        }
    }

    internal sealed record SampleSettings(string? SampleSetting);

    internal static class WorkerConfigurationExtensions
    {
        /// <summary>
        /// Calling ConfigureFunctionsWorkerDefaults() configures the Functions Worker to use System.Text.Json for all JSON
        /// serialization and sets JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        /// This method uses DI to modify the JsonSerializerOptions. Call /api/HttpFunction to see the changes.
        /// </summary>
        public static IFunctionsWorkerApplicationBuilder ConfigureSystemTextJson(this IFunctionsWorkerApplicationBuilder builder)
        {
            builder.Services.Configure<JsonSerializerOptions>(jsonSerializerOptions =>
            {
                jsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                jsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                jsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;

                // override the default value
                jsonSerializerOptions.PropertyNameCaseInsensitive = false;
            });

            return builder;
        }

        /// <summary>
        /// The functions worker uses the Azure SDK's ObjectSerializer to abstract away all JSON serialization. This allows you to
        /// swap out the default System.Text.Json implementation for the Newtonsoft.Json implementation.
        /// To do so, add the Microsoft.Azure.Core.NewtonsoftJson nuget package and then update the WorkerOptions.Serializer property.
        /// This method updates the Serializer to use Newtonsoft.Json. Call /api/HttpFunction to see the changes.
        /// </summary>
        public static IFunctionsWorkerApplicationBuilder UseNewtonsoftJson(this IFunctionsWorkerApplicationBuilder builder)
        {
            builder.Services.Configure<WorkerOptions>(workerOptions =>
            {
                var settings = NewtonsoftJsonObjectSerializer.CreateJsonSerializerSettings();
                settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                settings.NullValueHandling = NullValueHandling.Ignore;

                workerOptions.Serializer = new NewtonsoftJsonObjectSerializer(settings);
            });

            return builder;
        }
    }
}
