// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Azure.WebJobs.Extensions.FunctionMetadataLoader;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[assembly: WebJobsStartup(typeof(Startup))]

namespace Microsoft.Azure.WebJobs.Extensions.FunctionMetadataLoader
{
    internal class Startup : IWebJobsConfigurationStartup
    {
        private const string WorkerConfigFile = "worker.config.json";
        private const string ExePathPropertyName = "defaultExecutablePath";
        private const string WorkerPathPropertyName = "defaultWorkerPath";
        private const string WorkerRootToken = "{WorkerRoot}";

        private static readonly string _dotnetIsolatedWorkerConfigPath = ConfigurationPath.Combine("languageWorkers", "dotnet-isolated", "workerDirectory");
        private static readonly string _dotnetIsolatedWorkerExePath = ConfigurationPath.Combine("languageWorkers", "dotnet-isolated", ExePathPropertyName);

        public void Configure(WebJobsBuilderContext context, IWebJobsConfigurationBuilder builder)
        {
            string appRootPath = context.ApplicationRootPath;

            // We need to adjust the path to the worker exe based on the root, if WorkerRootToken is found.
            WorkerConfigDescription newWorkerDescription = GetUpdatedWorkerDescription(appRootPath);

            builder.ConfigurationBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                { _dotnetIsolatedWorkerConfigPath, appRootPath },
                { _dotnetIsolatedWorkerExePath, newWorkerDescription.DefaultExecutablePath! }
            });

            Environment.SetEnvironmentVariable("DOTNET_NOLOGO", "true");
            Environment.SetEnvironmentVariable("DOTNET_CLI_TELEMETRY_OPTOUT", "true");
            Environment.SetEnvironmentVariable("DOTNET_SKIP_FIRST_TIME_EXPERIENCE", "true");
        }

        public void Configure(IWebJobsBuilder builder)
        {
            // This will not be called.
        }

        private static WorkerConfigDescription GetUpdatedWorkerDescription(string appRootPath)
        {
            string fullPathToWorkerConfig = Path.Combine(appRootPath, WorkerConfigFile);
            WorkerConfigDescription workerDescription = GetWorkerConfigDescription(fullPathToWorkerConfig);

            if (string.IsNullOrEmpty(workerDescription.DefaultExecutablePath))
            {
                throw new InvalidOperationException($"The property '{ExePathPropertyName}' is required in '{fullPathToWorkerConfig}'.");
            }

            UpdateExecutablePath(workerDescription, appRootPath);

            return workerDescription;
        }

        private static void UpdateExecutablePath(WorkerConfigDescription workerDescription, string appRootPath)
        {
            string exePath = workerDescription.DefaultExecutablePath!;

            // Translate '{WorkerRoot}myExe' to '<app-path>\myExe.exe' for Windows, and '<app-path>/myExe' for Unix.
            if (HasWorkerRootToken(exePath))
            {
                exePath = AddWorkerRootPath(exePath, appRootPath);

                if (!File.Exists(exePath))
                {
                    throw new FileNotFoundException($"The file '{exePath}' was not found.");
                }
            }

            workerDescription.DefaultExecutablePath = exePath;
        }

        private static WorkerConfigDescription GetWorkerConfigDescription(string workerConfigPath)
        {
            if (!File.Exists(workerConfigPath))
            {
                throw new FileNotFoundException($"The file '{workerConfigPath}' was not found.");
            }

            WorkerConfigDescription workerDescription;

            using (var fs = File.OpenText(workerConfigPath))
            using (var js = new JsonTextReader(fs))
            {
                JObject workerDescriptionJObject = (JObject)JToken.ReadFrom(js)["description"];
                workerDescription = workerDescriptionJObject.ToObject<WorkerConfigDescription>();

                if (workerDescription is null)
                {
                    throw new InvalidOperationException($"The property 'description' is required in '{workerConfigPath}'.");
                }
            }

            return workerDescription;
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

            return AddExeExtensionIfWindows(execPath);
        }

        private static string AddExeExtensionIfWindows(string file)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !file.EndsWith(".exe"))
            {
                file += ".exe";
            }

            return file;
        }

        private class WorkerConfigDescription
        {
            [JsonProperty(ExePathPropertyName)]
            public string? DefaultExecutablePath { get; set; }

            [JsonProperty(WorkerPathPropertyName)]
            public string? DefaultWorkerPath { get; set; }
        }
    }
}
