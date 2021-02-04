// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.Azure.Functions.Worker.Sdk
{
    public class GenerateFunctionMetadata : Task
    {
        [Required]
        public string? AssemblyPath { get; set; }

        [Required]
        public string? OutputPath { get; set; }

        [Required]
        public string? ExtensionsCsProjFilePath { get; set; }

        [Required]
        public ITaskItem[]? ReferencePaths { get; set; }

        public override bool Execute()
        {
            var functionGenerator = new FunctionMetadataGenerator(MSBuildLogger);
            var functions = functionGenerator.GenerateFunctionMetadata(AssemblyPath!, ReferencePaths.Select(p => p.ItemSpec));

            var extensions = functionGenerator.Extensions;
            var extensionsCsProjGenerator = new ExtensionsCsprojGenerator(extensions, ExtensionsCsProjFilePath!);

            extensionsCsProjGenerator.Generate();

            FunctionMetadataJsonWriter.WriteMetadata(functions, OutputPath!);

            return true;
        }

        private void MSBuildLogger(TraceLevel level, string message)
        {
            switch (level)
            {
                case TraceLevel.Error:
                    Log.LogError(message);
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
