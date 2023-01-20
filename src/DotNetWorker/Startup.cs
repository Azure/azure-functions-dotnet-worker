#if NET5_0_OR_GREATER

using System;
using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Core.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Azure.Functions.Worker
{
    internal class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseAspNetHttpForwarderMiddleware();
        }

        public void ConfigureServices(IServiceCollection services)
        {
        }
    }
}

#endif
