using System;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Azure.Functions.DotNetWorker.Configuration
{
    public static class HostBuilderExtensions
    {
        public static HostBuilder ConfigureDotNetWorker(this HostBuilder builder)
        {
            return builder.ConfigureDotNetWorker(o => { });
        }

        public static HostBuilder ConfigureDotNetWorker(this HostBuilder builder, Action<IDotNetApplicationBuilder> configure)
        {
            return builder.ConfigureDotNetWorker(configure);
        }

        public static IHostBuilder ConfigureDotNetWorker(this IHostBuilder builder, Action<IDotNetApplicationBuilder> configure, Action<DotNetWorkerOptions> configureOptions)
        {
            return builder.ConfigureDotNetWorker((context, b) => configure(b), configureOptions);
        }

        public static IHostBuilder ConfigureDotNetWorker(this IHostBuilder builder, Action<HostBuilderContext, IDotNetApplicationBuilder> configure)
        {
            return builder.ConfigureDotNetWorker(configure, o => { });
        }
        public static IHostBuilder ConfigureDotNetWorker(this IHostBuilder builder, Action<HostBuilderContext, IDotNetApplicationBuilder> configure, Action<DotNetWorkerOptions> configureOptions)
        {
            builder.ConfigureServices((context, services) =>
            {
                IDotNetApplicationBuilder appBuilder = services.AddDotNetWorker(configureOptions);

                configure(context, appBuilder);
            });

            return builder;
        }
    }
}
