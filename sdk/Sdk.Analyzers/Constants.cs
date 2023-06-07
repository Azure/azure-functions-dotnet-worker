// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.Sdk.Analyzers
{
    internal static class Constants
    {
        internal static class Types
        {
            public const string WorkerFunctionAttribute = "Microsoft.Azure.Functions.Worker.FunctionAttribute";
            public const string WebJobsBindingAttribute = "Microsoft.Azure.WebJobs.Description.BindingAttribute";
            public const string SupportsDeferredBindingAttribute = "Microsoft.Azure.Functions.Worker.Extensions.Abstractions.SupportsDeferredBindingAttribute";
            public const string InputBindingAttribute = "Microsoft.Azure.Functions.Worker.Extensions.Abstractions.InputBindingAttribute";
            public const string TriggerBindingAttribute = "Microsoft.Azure.Functions.Worker.Extensions.Abstractions.TriggerBindingAttribute";
            public const string InputConverterAttribute = "Microsoft.Azure.Functions.Worker.Converters.InputConverterAttribute";
            public const string AllowConverterFallbackAttribute = "Microsoft.Azure.Functions.Worker.Converters.AllowConverterFallbackAttribute";
            public const string SupportedConverterTypeAttribute = "Microsoft.Azure.Functions.Worker.Converters.SupportedConverterTypeAttribute";

            // System types
            internal const string TaskType = "System.Threading.Tasks.Task";
        }

        internal static class DiagnosticsCategories
        {
            public const string Usage = "Usage";
        }
    }
}
