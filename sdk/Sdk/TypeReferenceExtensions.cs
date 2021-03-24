// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Mono.Cecil;

namespace Microsoft.Azure.Functions.Worker.Sdk
{
    internal static class TypeReferenceExtensions
    {
        public static string GetReflectionFullName(this TypeReference typeRef)
        {
            return typeRef.FullName.Replace('/', '+');
        }
    }
}
