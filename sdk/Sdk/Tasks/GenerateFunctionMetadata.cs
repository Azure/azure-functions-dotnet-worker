// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.Azure.Functions.Worker.Sdk.Tasks
{
#if NET472
    [LoadInSeparateAppDomain]
    public class GenerateFunctionMetadata : AppDomainIsolatedTask
#else
    public class GenerateFunctionMetadata : Task
#endif
    {
        private enum ExtensionsMode
        {
            Default = 0,
            ValidateExternal = 1,
            GenerateExternal = 2,
        }

        [Required]
        public string? AssemblyPath { get; set; }

        [Required]
        public string? OutputPath { get; set; }

        [Required]
        public string? ExtensionsCsProjFilePath { get; set; }

        public string? ExtensionsCsProjMode { get; set; }

        [Required]
        public ITaskItem[]? ReferencePaths { get; set; }

        [Required]
        public string? AzureFunctionsVersion { get; set; }

        [Required]
        public string? TargetFrameworkIdentifier { get; set; }

        [Required]
        public string? TargetFrameworkVersion { get; set; }

        public override bool Execute()
        {
            try
            {
                var functionGenerator = new FunctionMetadataGenerator(MSBuildLogger);

                IEnumerable<SdkFunctionMetadata> functions = functionGenerator.GenerateFunctionMetadata(AssemblyPath!, ReferencePaths ?? Enumerable.Empty<ITaskItem>());
                IDictionary<string, string> extensions = functionGenerator.Extensions;

                
                if (!Enum.TryParse(ExtensionsCsProjMode, true, out ExtensionsMode mode))
                {
                    Log.LogError($"Invalid value '{ExtensionsCsProjMode}' for ExtensionsCsProjMode. Valid values are 'Default', 'ValidateExternal', or 'GenerateExternal'.");
                    return false;
                }

                var extensionsCsProjGenerator = new ExtensionsCsprojGenerator(
                    extensions,
                    ExtensionsCsProjFilePath!,
                    AzureFunctionsVersion!,
                    TargetFrameworkIdentifier!,
                    TargetFrameworkVersion!)
                {
                    Disclaimer = mode == ExtensionsMode.Default ? Constants.ExtensionCsProjDisclaimer : Constants.ExternalExtensionCsProjDisclaimer,
                };

                if (mode == ExtensionsMode.ValidateExternal)
                {
                    // Validate only mode. Check if the contents on disk match what would be generated.
                    if (File.Exists(ExtensionsCsProjFilePath!))
                    {
                        string content = File.ReadAllText(ExtensionsCsProjFilePath!);
                        if (string.IsNullOrEmpty(content))
                        {
                            Log.LogError($"The extensions csproj file '{ExtensionsCsProjFilePath}' is empty.");
                            return false;
                        }
                        else if (!string.Equals(content, extensionsCsProjGenerator.GetCsProjContent(), StringComparison.Ordinal))
                        {
                            Log.LogError($"The extensions csproj file '{ExtensionsCsProjFilePath}' does not match the expected content. Re-run `dotnet build -t:GenerateExtensionProject` to update the csproj.");
                            return false;
                        }
                    }
                    else
                    {
                        Log.LogError($"The extensions csproj file '{ExtensionsCsProjFilePath}' does not exist.");
                        return false;
                    }
                }
                else
                {
                    Log.LogMessage($"Generating extensions csproj file '{ExtensionsCsProjFilePath}'.");
                    extensionsCsProjGenerator.Generate();
                }

                if (mode == ExtensionsMode.GenerateExternal)
                {
                    // Generate the Directory.Build.props and Directory.Build.targets files.
                    Log.LogMessage("Generating Directory.Build.props and Directory.Build.targets files.");
                    extensionsCsProjGenerator.GenerateDirectoryBuild();
                }

                WriteMetadataWithRetry(functions);
            }
            catch (FunctionsMetadataGenerationException)
            {
                Log.LogError($"Unable to build Azure Functions metadata for {AssemblyPath}");
                return false;
            }

            return true;
        }

        private void WriteMetadataWithRetry(IEnumerable<SdkFunctionMetadata> functions)
        {
            int attempt = 0;
            while (attempt < 10)
            {
                try
                {
                    FunctionMetadataJsonWriter.WriteMetadata(functions, OutputPath!);
                    break;
                }
                catch (IOException ex)
                {
                    attempt++;
                    if (attempt == 10)
                    {
                        Log.LogErrorFromException(ex);
                        throw new FunctionsMetadataGenerationException();
                    }

                    string message = $"Could not write function metadata. Error: '{ex.Message}'. Beginning retry {attempt} of 10 in 1000ms.";
                    Log.LogWarning(message);
                    Thread.Sleep(1000);
                }
            }
        }

        private void MSBuildLogger(TraceLevel level, string message, string path)
        {
            switch (level)
            {
                case TraceLevel.Error:
                    Log.LogError(null, null, null, file: path, 0, 0, 0, 0, message: message);
                    break;
                case TraceLevel.Info:
                    Log.LogMessage(message);
                    break;
                case TraceLevel.Verbose:
                    Log.LogMessage(MessageImportance.Low, message);
                    break;
                case TraceLevel.Warning:
                    Log.LogWarning(message);
                    break;
                case TraceLevel.Off:
                default:
                    break;
            }
        }
    }
}
