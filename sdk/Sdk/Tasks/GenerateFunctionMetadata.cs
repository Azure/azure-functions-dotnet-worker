// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Linq;
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
        [Required]
        public string? AssemblyPath { get; set; }

        [Required]
        public string? OutputPath { get; set; }

        [Required]
        public string? ExtensionsCsProjFilePath { get; set; }

        [Required]
        public ITaskItem[]? ReferencePaths { get; set; }

        [Required]
        public string? AzureFunctionsVersion { get; set; }

        public override bool Execute()
        {
            try
            {
                var functionGenerator = new FunctionMetadataGenerator(MSBuildLogger);

                var functions = functionGenerator.GenerateFunctionMetadata(AssemblyPath!, ReferencePaths.Select(p => p.ItemSpec));

                var extensions = functionGenerator.Extensions;
                var extensionsCsProjGenerator = new ExtensionsCsprojGenerator(extensions, ExtensionsCsProjFilePath!, AzureFunctionsVersion!);

                extensionsCsProjGenerator.Generate();

                FunctionMetadataJsonWriter.WriteMetadata(functions, OutputPath!);
            }
            catch (FunctionsMetadataGenerationException)
            {
                Log.LogError($"Unable to build Azure Functions metadata for {AssemblyPath}");
                return false;
            }

            return true;
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
