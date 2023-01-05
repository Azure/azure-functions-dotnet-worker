// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.Sdk.Generators
{
    internal static class Constants
    {
        public static class BuildProperties
        {
            internal const string EnableSourceGenProp = "build_property.FunctionsMetadataSourceGen_Enabled";
        }

        public static class FileNames
        {
            internal const string GeneratedFunctionMetadata = "GeneratedFunctionMetadataProvider.g.cs";
        }

        public static class Types
        {
            // Our types
            internal const string BindingAttribute = "Microsoft.Azure.Functions.Worker.Extensions.Abstractions.BindingAttribute";
            internal const string OutputBindingAttribute = "Microsoft.Azure.Functions.Worker.Extensions.Abstractions.OutputBindingAttribute";
            internal const string FunctionName = "Microsoft.Azure.Functions.Worker.FunctionAttribute";
            internal const string HttpResponse = "Microsoft.Azure.Functions.Worker.Http.HttpResponseData";
            internal const string HttpTriggerBinding = "Microsoft.Azure.Functions.Worker.HttpTriggerAttribute";
            internal const string EventHubsTrigger = "Microsoft.Azure.Functions.Worker.EventHubTriggerAttribute";
            internal const string BindingPropertyNameAttribute = "Microsoft.Azure.Functions.Worker.Extensions.Abstractions.BindingPropertyNameAttribute";
            internal const string DefaultValue = "Microsoft.Azure.Functions.Worker.Extensions.Abstractions.DefaultValueAttribute";

            // System types
            internal const string IEnumerable = "System.Collections.IEnumerable";
            internal const string IEnumerableGeneric = "System.Collections.Generic.IEnumerable`1";
            internal const string IEnumerableOfString = "System.Collections.Generic.IEnumerable`1<System.String>";
            internal const string IEnumerableOfBinary = "System.Collections.Generic.IEnumerable`1<System.Byte[]>";
            internal const string IEnumerableOfT = "System.Collections.Generic.IEnumerable`1<T>";
            internal const string IEnumerableOfKeyValuePair = "System.Collections.Generic.IEnumerable`1<System.Collections.Generic.KeyValuePair`2<TKey,TValue>>";
            internal const string String = "System.String";
            internal const string ByteArray = "System.Byte[]";
            internal const string ByteStruct = "System.Byte";
            internal const string TaskGeneric = "System.Threading.Tasks.Task`1";
            internal const string Task = "System.Threading.Tasks.Task";
            internal const string Void = "System.Void";
            internal const string ReadOnlyMemoryOfBytes = "System.ReadOnlyMemory`1<System.Byte>";
            internal const string LookupGeneric = "System.Linq.Lookup`2";
            internal const string DictionaryGeneric = "System.Collections.Generic.Dictionary`2";
        }
        
        public static class FunctionMetadataBindingProps {
            internal const string ReturnBindingName = "$return";
            internal const string HttpResponseBindingName = "HttpResponse";
            internal const string IsBatchedKey = "IsBatched";
        }
    }
}
