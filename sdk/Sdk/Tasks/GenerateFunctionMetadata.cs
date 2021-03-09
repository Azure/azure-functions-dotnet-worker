// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.Azure.Functions.Worker.Sdk
{
#if NET46
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
        public string? ExtensionsTargetFramework { get; set; }

        public override bool Execute()
        {
            var functionGenerator = new FunctionMetadataGenerator(MSBuildLogger);
            var functions = functionGenerator.GenerateFunctionMetadata(AssemblyPath!, ReferencePaths.Select(p => p.ItemSpec));

            var extensions = functionGenerator.Extensions;
            var extensionsCsProjGenerator = new ExtensionsCsprojGenerator(extensions, ExtensionsCsProjFilePath!, ExtensionsTargetFramework!);

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
