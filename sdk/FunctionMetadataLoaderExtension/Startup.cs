// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs.Extensions.FunctionMetadataLoader;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Azure.WebJobs.Script.Description;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: WebJobsStartup(typeof(Startup))]

namespace Microsoft.Azure.WebJobs.Extensions.FunctionMetadataLoader
{
    internal class Startup : IWebJobsStartup2, IWebJobsConfigurationStartup
    {
        public void Configure(WebJobsBuilderContext context, IWebJobsConfigurationBuilder builder)
        {
            builder.ConfigurationBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                { "languageWorkers:dotnet-isolated:workerDirectory", context.ApplicationRootPath }
            });
        }

        public void Configure(WebJobsBuilderContext context, IWebJobsBuilder builder)
        {
            string appRootPath = context.ApplicationRootPath;
            builder.Services.AddOptions<FunctionMetadataJsonReaderOptions>().Configure(o => o.FunctionMetadataFileDrectory = appRootPath);
            builder.Services.AddSingleton<FunctionMetadataJsonReader>();
            builder.Services.AddSingleton<IFunctionProvider, JsonFunctionProvider>();
        }

        public void Configure(IWebJobsBuilder builder)
        {
            // This will not be called.
        }
    }
}
