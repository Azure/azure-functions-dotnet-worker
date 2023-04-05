// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Microsoft.Azure.Functions.Worker.Sdk.Generators
{
    // Trimmed version of https://github.com/dotnet/roslyn/blob/main/src/Features/Core/Portable/MakeMethodAsynchronous/AbstractMakeMethodAsynchronousCodeFixProvider.KnownTypes.cs
    internal readonly struct KnownTypes
    {
        internal readonly INamedTypeSymbol TaskType;
        internal readonly INamedTypeSymbol TaskOfTType;
        internal readonly INamedTypeSymbol ValueTaskType;
        internal readonly INamedTypeSymbol ValueTaskOfTTypeOpt;

        internal readonly INamedTypeSymbol IEnumerable;
        internal readonly INamedTypeSymbol IEnumerableGeneric; // IEnumerable<T>
        internal readonly INamedTypeSymbol IEnumerableOfKeyValuePair;
        internal readonly INamedTypeSymbol StringType;
        internal readonly INamedTypeSymbol ByteArray;
        internal readonly INamedTypeSymbol ByteType;
        internal readonly INamedTypeSymbol VoidType;
        internal readonly INamedTypeSymbol ReadOnlyMemoryOfBytes;
        internal readonly INamedTypeSymbol LookupGeneric;
        internal readonly INamedTypeSymbol DictionaryGeneric;

        internal KnownTypes(Compilation compilation)
        {
            TaskType = compilation.GetTypeByMetadataName(typeof(Task).FullName)!;
            TaskOfTType = compilation.GetTypeByMetadataName(typeof(Task<>).FullName)!;
            ValueTaskType = compilation.GetTypeByMetadataName(typeof(ValueTask).FullName)!;
            ValueTaskOfTTypeOpt = compilation.GetTypeByMetadataName(typeof(ValueTask<>).FullName)!;

            IEnumerable = compilation.GetTypeByMetadataName(typeof(IEnumerable).FullName)!;
            IEnumerableGeneric = compilation.GetTypeByMetadataName(typeof(IEnumerable<>).FullName)!;
            IEnumerableOfKeyValuePair = compilation.GetTypeByMetadataName(Constants.Types.IEnumerableOfKeyValuePair)!; // TODO: Revisit using typeof instead of string constant
            StringType = compilation.GetTypeByMetadataName(typeof(string).FullName)!;
            ByteArray = compilation.GetTypeByMetadataName(typeof(byte[]).FullName)!;
            ByteType = compilation.GetTypeByMetadataName(typeof(byte).FullName)!;
            VoidType = compilation.GetTypeByMetadataName(typeof(void).FullName)!;
            ReadOnlyMemoryOfBytes = compilation.GetTypeByMetadataName(typeof(ReadOnlyMemory<byte>).FullName)!;
            LookupGeneric = compilation.GetTypeByMetadataName(typeof(ILookup<,>).FullName)!;
            DictionaryGeneric = compilation.GetTypeByMetadataName(typeof(IDictionary<,>).FullName)!;
        }
    }
}
