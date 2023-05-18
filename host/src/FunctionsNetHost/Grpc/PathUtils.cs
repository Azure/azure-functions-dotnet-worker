// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text.Json.Nodes;

namespace FunctionsNetHost.Grpc
{
    internal class PathUtils
    {
        internal static string GetApplicationExePath(string applicationDirectory)
        {
            var workerConfigPath = Path.Combine(applicationDirectory, "worker.config.json");

            if (!File.Exists(workerConfigPath))
            {
                throw new FileNotFoundException($"worker.config.json file not found", fileName: workerConfigPath);
            }

            Logger.Log($"workerConfigPath:{workerConfigPath}");

            var jsonString = File.ReadAllText(workerConfigPath);
            var workerConfigJsonNode = JsonNode.Parse(jsonString)!;
            var executableName = workerConfigJsonNode["description"]!["defaultWorkerPath"]!.ToString();

            return Path.Combine(applicationDirectory, executableName);
        }
    }
}
