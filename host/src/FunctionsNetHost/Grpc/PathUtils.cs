// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text.Json;
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
        internal static string? GetApplicationExePath(string applicationDirectory)
        {
            string jsonString = string.Empty;
            string workerConfigPath = string.Empty;
            try
            {
                workerConfigPath = Path.Combine(applicationDirectory, "worker.config.json");

                jsonString = File.ReadAllText(workerConfigPath);
                var workerConfigJsonNode = JsonNode.Parse(jsonString)!;
                var executableName = workerConfigJsonNode["description"]?["defaultWorkerPath"]?.ToString();

                if (executableName == null)
                {
                    Logger.Log($"Invalid worker configuration. description > defaultWorkerPath property value is null. jsonString:{jsonString}");
                    return null;
                }

                return Path.Combine(applicationDirectory, executableName);
            }
            catch (FileNotFoundException ex)
            {
                Logger.Log($"{workerConfigPath} file not found.{ex}");
                return null;
            }
            catch (JsonException ex)
            {
                Logger.Log($"Error parsing JSON in GetApplicationExePath.{ex}. jsonString:{jsonString}");
                return null;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error in GetApplicationExePath.{ex}. jsonString:{jsonString}");
                return null;
            }
        }
    }
}
