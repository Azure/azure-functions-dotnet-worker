﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.Sdk
{
    internal static class Constants
    {
        // Our types
        internal const string BindingType = "Microsoft.Azure.Functions.Worker.Extensions.Abstractions.BindingAttribute";
        internal const string OutputBindingType = "Microsoft.Azure.Functions.Worker.Extensions.Abstractions.OutputBindingAttribute";
        internal const string FunctionNameType = "Microsoft.Azure.Functions.Worker.FunctionAttribute";
        internal const string ExtensionsInformationType = "Microsoft.Azure.Functions.Worker.Extensions.Abstractions.ExtensionInformationAttribute";
        internal const string HttpResponseType = "Microsoft.Azure.Functions.Worker.Http.HttpResponseData";
        internal const string HttpResultAttributeType = "Microsoft.Azure.Functions.Worker.HttpResultAttribute";
        internal const string DefaultValueAttributeType = "Microsoft.Azure.Functions.Worker.Extensions.Abstractions.DefaultValueAttribute";
        internal const string FixedDelayRetryAttributeType = "Microsoft.Azure.Functions.Worker.FixedDelayRetryAttribute";
        internal const string ExponentialBackoffRetryAttributeType = "Microsoft.Azure.Functions.Worker.ExponentialBackoffRetryAttribute";
        internal const string BindingPropertyNameAttributeType = "Microsoft.Azure.Functions.Worker.Extensions.Abstractions.BindingPropertyNameAttribute";
        internal const string SupportsDeferredBindingAttributeType = "Microsoft.Azure.Functions.Worker.Extensions.Abstractions.SupportsDeferredBindingAttribute";
        internal const string InputConverterAttributeType = "Microsoft.Azure.Functions.Worker.Converters.InputConverterAttribute";
        internal const string SupportedTargetTypeAttributeType = "Microsoft.Azure.Functions.Worker.Converters.SupportedTargetTypeAttribute";

        // System types
        internal const string IEnumerableType = "System.Collections.IEnumerable";
        internal const string IEnumerableGenericType = "System.Collections.Generic.IEnumerable`1";
        internal const string IEnumerableOfStringType = "System.Collections.Generic.IEnumerable`1<System.String>";
        internal const string IEnumerableOfBinaryType = "System.Collections.Generic.IEnumerable`1<System.Byte[]>";
        internal const string IEnumerableOfT = "System.Collections.Generic.IEnumerable`1<T>";
        internal const string IEnumerableOfKeyValuePair = "System.Collections.Generic.IEnumerable`1<System.Collections.Generic.KeyValuePair`2<TKey,TValue>>";
        internal const string GenericIEnumerableArgumentName = "T";
        internal const string StringType = "System.String";
        internal const string ByteArrayType = "System.Byte[]";
        internal const string LookupGenericType = "System.Linq.Lookup`2";
        internal const string DictionaryGenericType = "System.Collections.Generic.Dictionary`2";
        internal const string TaskGenericType = "System.Threading.Tasks.Task`1";
        internal const string TaskType = "System.Threading.Tasks.Task";
        internal const string VoidType = "System.Void";
        internal const string ReadOnlyMemoryOfBytes = "System.ReadOnlyMemory`1<System.Byte>";

        internal const string ReturnBindingName = "$return";
        internal const string HttpTriggerBindingType = "httpTrigger";
        internal const string IsBatchedKey = "IsBatched";

        // NetFramework
        internal const string NetCoreApp31 = "netcoreapp3.1";
        internal const string Net80 = "net8.0";
        internal const string NetCoreApp = ".NETCoreApp";
        internal const string NetCoreVersion31 = "v3.1";
        internal const string NetCoreVersion8 = "v8.0";
        internal const string AzureFunctionsVersion3 = "v3";

        // Binding directions
        internal const string OutputBindingDirection = "Out";
        internal const string InputBindingDirection = "In";

        // Binding properties
        internal const string SupportsDeferredBindingProperty = "SupportsDeferredBinding";
    }
}
