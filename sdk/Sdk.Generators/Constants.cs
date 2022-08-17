// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.Sdk.Generators
{
    internal static class Constants
    {
        // Our types
        internal const string BindingAttributeType = "Microsoft.Azure.Functions.Worker.Extensions.Abstractions.BindingAttribute";
        internal const string OutputBindingAttributeType = "Microsoft.Azure.Functions.Worker.Extensions.Abstractions.OutputBindingAttribute";
        internal const string FunctionNameType = "Microsoft.Azure.Functions.Worker.FunctionAttribute";
        internal const string HttpResponseType = "Microsoft.Azure.Functions.Worker.Http.HttpResponseData";

        // System types
        internal const string GenericIEnumerableArgumentName = "T";
        internal const string StringType = "System.String";
        internal const string ByteArrayType = "System.Byte[]";
        internal const string TaskType = "System.Threading.Tasks.Task";
        internal const string VoidType = "System.Void";
        internal const string ReadOnlyMemoryOfBytes = "System.ReadOnlyMemory`1<System.Byte>";

        internal const string ReturnBindingName = "$return";
        internal const string HttpTriggerBindingType = "Microsoft.Azure.Functions.Worker.HttpTriggerAttribute";
        internal const string IsBatchedKey = "IsBatched";
    }
}
