// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text.Json;

namespace FunctionsNetHost.Grpc
{
    internal static class WorkerConfigUtils
    {
        /// <summary>
        /// Builds and returns an instance of <see cref="WorkerConfig"/> from the worker.config.json file if present in the application directory.
        /// </summary>
        /// <param name="applicationDirectory">The directory where function app deployed payload is present.</param>
        internal static async Task<WorkerConfig?> GetWorkerConfig(string applicationDirectory)
        {
            string workerConfigPath = string.Empty;

            try
            {
                workerConfigPath = Path.Combine(applicationDirectory, "worker.config.json");

                using Stream stream = File.OpenRead(workerConfigPath);
                var workerConfig = await JsonSerializer.DeserializeAsync(stream, WorkerConfigSerializerContext.Default.WorkerConfig);

                return workerConfig;
            }
            catch (FileNotFoundException)
            {
                Logger.Log($"worker.config.json not found at {workerConfigPath}. This may indicate missing app payload.");
                return null;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error in WorkerConfigUtils.GetWorkerConfig.{ex}");
                return null;
            }
        }
    }
}
