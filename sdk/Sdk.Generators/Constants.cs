﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.Sdk.Generators
{
    internal static class Constants
    {
        internal static class Languages
        {
            internal const string DotnetIsolated = "dotnet-isolated";
        }

        internal static class BuildProperties
        {
            internal const string EnableSourceGen = "build_property.FunctionsEnableMetadataSourceGen";
            internal const string EnablePlaceholder = "build_property.FunctionsEnableExecutorSourceGen";
        }

        internal static class FileNames
        {
            internal const string GeneratedFunctionMetadata = "GeneratedFunctionMetadataProvider.g.cs";
            internal const string GeneratedFunctionExecutor = "GeneratedFunctionExecutor.g.cs";
        }

        internal static class Types
        {
            // Azure Functions worker types
            internal const string FunctionName = "Microsoft.Azure.Functions.Worker.FunctionAttribute";
            internal const string BindingAttribute = "Microsoft.Azure.Functions.Worker.Extensions.Abstractions.BindingAttribute";
            internal const string OutputBindingAttribute = "Microsoft.Azure.Functions.Worker.Extensions.Abstractions.OutputBindingAttribute";
            internal const string BindingPropertyNameAttribute = "Microsoft.Azure.Functions.Worker.Extensions.Abstractions.BindingPropertyNameAttribute";
            internal const string DefaultValue = "Microsoft.Azure.Functions.Worker.Extensions.Abstractions.DefaultValueAttribute";

            internal const string HttpResponse = "Microsoft.Azure.Functions.Worker.Http.HttpResponseData";
            internal const string HttpTriggerBinding = "Microsoft.Azure.Functions.Worker.HttpTriggerAttribute";


            // System types
            internal const string IEnumerableOfKeyValuePair = "System.Collections.Generic.IEnumerable`1<System.Collections.Generic.KeyValuePair`2<TKey,TValue>>";
        }
        
        internal static class FunctionMetadataBindingProps {
            internal const string ReturnBindingName = "$return";
            internal const string HttpResponseBindingName = "HttpResponse";
            internal const string IsBatchedKey = "IsBatched";
        }
    }
}
