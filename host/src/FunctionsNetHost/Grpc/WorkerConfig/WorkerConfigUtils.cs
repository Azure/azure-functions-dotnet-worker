// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text.Json;

namespace FunctionsNetHost.Grpc
{
    internal static class WorkerConfigUtils
    {
        /// <summary>
        /// Build and returns an instance of <see cref="WorkerConfig"/> from the worker.config.json file if present in the application directory.
        /// </summary>
        /// <param name="applicationDirectory">The directory where function app deployed payload is present.</param>
        internal static WorkerConfig? GetWorkerConfig(string applicationDirectory)
        {
            try
            {
                var workerConfigPath = Path.Combine(applicationDirectory, "worker.config.json");

                if (!File.Exists(workerConfigPath))
                {
                    Logger.Log($"worker.config.json not found at {workerConfigPath}. This may indicate missing app payload.");
                    return null;
                }

                var jsonString = File.ReadAllText(workerConfigPath);
                return JsonSerializer.Deserialize(jsonString, WorkerConfigSerializerContext.Default.WorkerConfig);
            }
            catch (Exception ex)
            {
                Logger.Log($"Error in WorkerConfigUtils.GetWorkerConfig.{ex}");
                return null;
            }
        }
    }
}
