// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Microsoft.Azure.Functions.Worker.Sdk.Generators
{
    internal static class GeneratorExecutionContextExtensions
    {
        /// <summary>
        /// Returns true if the source generator is running in the context of an "Azure Function" project.
        /// </summary>
        internal static bool IsRunningInAzureFunctionProject(this GeneratorExecutionContext context)
        {
            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(Constants.BuildProperties.FunctionsExecutionModel, out var value))
            {
                return string.Equals(value, Constants.Isolated);
            }

            return false;
        }
    }
}
