// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Azure.WebJobs.Extensions.FunctionMetadataLoader;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Azure.WebJobs.Script.Description;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[assembly: WebJobsStartup(typeof(Startup))]

namespace Microsoft.Azure.WebJobs.Extensions.FunctionMetadataLoader
{
    internal class Startup : IWebJobsStartup2, IWebJobsConfigurationStartup
    {
        private const string _workerConfigFile = "worker.config.json";
        private const string _exePathPropertyName = "defaultExecutablePath";
        private static readonly string _dotnetIsolatedWorkerConfigPath = ConfigurationPath.Combine("languageWorkers", "dotnet-isolated", "workerDirectory");
        private static readonly string _dotnetIsolatedWorkerExePath = ConfigurationPath.Combine("languageWorkers", "dotnet-isolated", "defaultExecutablePath");

        public void Configure(WebJobsBuilderContext context, IWebJobsConfigurationBuilder builder)
        {
            string appRootPath = context.ApplicationRootPath;

            // We need to adjust the path to the worker exe based on the root.
            string exePath = GetPathToExe(appRootPath);

            builder.ConfigurationBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                { _dotnetIsolatedWorkerConfigPath, appRootPath },
                { _dotnetIsolatedWorkerExePath, exePath }
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

        internal static string GetPathToExe(string appRootPath)
        {
            string fullPathToWorkerConfig = Path.Combine(appRootPath, _workerConfigFile);

            if (!File.Exists(fullPathToWorkerConfig))
            {
                throw new FileNotFoundException($"The file '{fullPathToWorkerConfig}' was not found.");
            }

            string? exePathString;

            using (var fs = File.OpenText(fullPathToWorkerConfig))
            {
                using (var js = new JsonTextReader(fs))
                {
                    JObject workerDescription = (JObject)JToken.ReadFrom(js)["description"];
                    if (!workerDescription.TryGetValue(_exePathPropertyName, out JToken exePathToken))
                    {
                        throw new InvalidOperationException($"The property '{_exePathPropertyName}' is required in '{fullPathToWorkerConfig}'.");
                    }

                    exePathString = Path.Combine(appRootPath, exePathToken.ToString());
                }
            }

            if (string.IsNullOrEmpty(exePathString))
            {
                throw new InvalidOperationException($"The property '{_exePathPropertyName}' in '{fullPathToWorkerConfig}' cannot be null or empty.");
            }

            if (!File.Exists(exePathString))
            {
                throw new FileNotFoundException($"The file '{exePathString}' was not found.");
            }

            return exePathString;
        }
    }
}
