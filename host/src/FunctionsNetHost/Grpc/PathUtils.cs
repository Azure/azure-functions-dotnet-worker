// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text.Json.Nodes;

namespace FunctionsNetHost.Grpc
{
    internal static class PathUtils
    {
        /// <summary>
        /// Gets the absolute path to worker application executable.
        /// Builds the path by reading the worker.config.json
        /// </summary>
        /// <param name="applicationDirectory">The FunctionAppDirectory value from environment reload request.</param>
        internal static string GetApplicationExePath(string applicationDirectory)
        {
            var workerConfigPath = Path.Combine(applicationDirectory, "worker.config.json");

            if (!File.Exists(workerConfigPath))
            {
                throw new FileNotFoundException($"worker.config.json file not found", fileName: workerConfigPath);
            }

            if (Logger.IsDebugLogEnabled)
            {
                Logger.LogDebug($"workerConfigPath:{workerConfigPath}");
            }

            var jsonString = File.ReadAllText(workerConfigPath);
            var workerConfigJsonNode = JsonNode.Parse(jsonString)!;
            var executableName = workerConfigJsonNode["description"]?["defaultWorkerPath"]?.ToString();
            
            if (executableName == null)
            {
                throw new InvalidOperationException("Invalid worker configuration.");
            }
            
            return Path.Combine(applicationDirectory, executableName);
        }
    }
}
