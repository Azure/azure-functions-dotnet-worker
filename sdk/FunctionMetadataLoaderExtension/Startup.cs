// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
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
        private const string WorkerConfigFile = "worker.config.json";
        private const string ExePathPropertyName = "defaultExecutablePath";
        private const string WorkerRootToken = "{WorkerRoot}";

        private static readonly string _dotnetIsolatedWorkerConfigPath = ConfigurationPath.Combine("languageWorkers", "dotnet-isolated", "workerDirectory");
        private static readonly string _dotnetIsolatedWorkerExePath = ConfigurationPath.Combine("languageWorkers", "dotnet-isolated", "defaultExecutablePath");

        public void Configure(WebJobsBuilderContext context, IWebJobsConfigurationBuilder builder)
        {
            string appRootPath = context.ApplicationRootPath;

            // We need to adjust the path to the worker exe based on the root, if WorkerRootToken is found.
            string exePath = GetUpdatedExeIfWorkerRootToken(appRootPath);

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

        internal static string GetUpdatedExeIfWorkerRootToken(string appRootPath)
        {
            string fullPathToWorkerConfig = Path.Combine(appRootPath, WorkerConfigFile);
            string? exeValue = GetExeValueFromWorkerConfig(fullPathToWorkerConfig);

            if (HasWorkerRootToken(exeValue))
            {
                exeValue = AddWorkerRootPath(exeValue, appRootPath);

                if (!File.Exists(exeValue))
                {
                    throw new FileNotFoundException($"The file '{exeValue}' was not found.");
                }
            }

            return exeValue;
        }

        private static string GetExeValueFromWorkerConfig(string workerConfigPath)
        {
            if (!File.Exists(workerConfigPath))
            {
                throw new FileNotFoundException($"The file '{workerConfigPath}' was not found.");
            }

            string? exePathString;
            using (var fs = File.OpenText(workerConfigPath))
            using (var js = new JsonTextReader(fs))
            {
                JObject workerDescription = (JObject)JToken.ReadFrom(js)["description"];
                if (!workerDescription.TryGetValue(ExePathPropertyName, out JToken exePathToken))
                {
                    throw new InvalidOperationException($"The property '{ExePathPropertyName}' is required in '{workerConfigPath}'.");
                }

                exePathString = exePathToken.ToString();
            }

            if (string.IsNullOrEmpty(exePathString))
            {
                throw new InvalidOperationException($"The property '{ExePathPropertyName}' in '{workerConfigPath}' cannot be null or empty.");
            }

            return exePathString;
        }

        private static bool HasWorkerRootToken(string exe)
        {
            return exe.Contains(WorkerRootToken);
        }

        private static string AddWorkerRootPath(string exe, string appRootPath)
        {
            string execName = exe.Replace(WorkerRootToken, string.Empty);

            if (string.IsNullOrEmpty(execName))
            {
                throw new InvalidOperationException($"The property '{ExePathPropertyName}' in '{WorkerConfigFile}' does not contain the executable file name.");
            }

            string execPath = Path.Combine(appRootPath, execName);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !execPath.EndsWith(".exe"))
            {
                execPath += ".exe";
            }

            return execPath;
        }
    }
}
