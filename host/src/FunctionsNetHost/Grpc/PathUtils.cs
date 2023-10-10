// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace FunctionsNetHost.Grpc
{
    internal static class PathUtils
    {
        public static WorkerConfig? GetWorkerConfig(string applicationDirectory)
        {
            string workerConfigPath = string.Empty;

            try
            {
                workerConfigPath = Path.Combine(applicationDirectory, "worker.config.json");

                if (!File.Exists(workerConfigPath))
                {
                    Logger.Log($"Worker config file not found at {workerConfigPath}");
                    return null;
                }

                var jsonString = File.ReadAllText(workerConfigPath);
                return JsonSerializer.Deserialize<WorkerConfig>(jsonString, WorkerConfigContext.Default.WorkerConfig);
            }
            catch (Exception ex)
            {
                Logger.Log($"Error in GetWorkerConfig.{ex}");
                return null;
            }
        }
    }

    public class WorkerDescription
    {
        public string? DefaultWorkerPath { set; get; }

        public bool IsSpecializable { set; get; }
    }

    public class WorkerConfig
    {
        public WorkerDescription? Description { set; get; }
    }

    [JsonSerializable(typeof(WorkerConfig))]
    [JsonSourceGenerationOptions(IncludeFields = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    internal partial class WorkerConfigContext : JsonSerializerContext { }
}
