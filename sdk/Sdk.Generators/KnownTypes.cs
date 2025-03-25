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
        private readonly Lazy<INamedTypeSymbol> _taskType;
        private readonly Lazy<INamedTypeSymbol> _taskOfTType;
        private readonly Lazy<INamedTypeSymbol> _valueTaskType;
        private readonly Lazy<INamedTypeSymbol> _valueTaskOfTTypeOpt;
        private readonly Lazy<INamedTypeSymbol> _iEnumerable;
        private readonly Lazy<INamedTypeSymbol> _iEnumerableGeneric;
        private readonly Lazy<INamedTypeSymbol> _iEnumerableOfKeyValuePair;
        private readonly Lazy<INamedTypeSymbol> _stringType;
        private readonly Lazy<INamedTypeSymbol> _byteArray;
        private readonly Lazy<INamedTypeSymbol> _byteType;
        private readonly Lazy<INamedTypeSymbol> _voidType;
        private readonly Lazy<INamedTypeSymbol> _readOnlyMemoryOfBytes;
        private readonly Lazy<INamedTypeSymbol> _lookupGeneric;
        private readonly Lazy<INamedTypeSymbol> _dictionaryGeneric;
        private readonly Lazy<INamedTypeSymbol> _obsoleteAttr;

        internal KnownTypes(Compilation compilation)
        {
            _taskType = new Lazy<INamedTypeSymbol>(() => compilation.GetTypeByMetadataName(typeof(Task).FullName)!);
            _taskOfTType = new Lazy<INamedTypeSymbol>(() => compilation.GetTypeByMetadataName(typeof(Task<>).FullName)!);
            _valueTaskType = new Lazy<INamedTypeSymbol>(() => compilation.GetTypeByMetadataName(typeof(ValueTask).FullName)!);
            _valueTaskOfTTypeOpt = new Lazy<INamedTypeSymbol>(() => compilation.GetTypeByMetadataName(typeof(ValueTask<>).FullName)!);
            _iEnumerable = new Lazy<INamedTypeSymbol>(() => compilation.GetTypeByMetadataName(typeof(IEnumerable).FullName)!);
            _iEnumerableGeneric = new Lazy<INamedTypeSymbol>(() => compilation.GetTypeByMetadataName(typeof(IEnumerable<>).FullName)!);
            _iEnumerableOfKeyValuePair = new Lazy<INamedTypeSymbol>(() => compilation.GetTypeByMetadataName(Constants.Types.IEnumerableOfKeyValuePair)!);
            _stringType = new Lazy<INamedTypeSymbol>(() => compilation.GetTypeByMetadataName(typeof(string).FullName)!);
            _byteArray = new Lazy<INamedTypeSymbol>(() => compilation.GetTypeByMetadataName(typeof(byte[]).FullName)!);
            _byteType = new Lazy<INamedTypeSymbol>(() => compilation.GetTypeByMetadataName(typeof(byte).FullName)!);
            _voidType = new Lazy<INamedTypeSymbol>(() => compilation.GetTypeByMetadataName(typeof(void).FullName)!);
            _readOnlyMemoryOfBytes = new Lazy<INamedTypeSymbol>(() => compilation.GetTypeByMetadataName(typeof(ReadOnlyMemory<byte>).FullName)!);
            _lookupGeneric = new Lazy<INamedTypeSymbol>(() => compilation.GetTypeByMetadataName(typeof(ILookup<,>).FullName)!);
            _dictionaryGeneric = new Lazy<INamedTypeSymbol>(() => compilation.GetTypeByMetadataName(typeof(IDictionary<,>).FullName)!);
            _obsoleteAttr = new Lazy<INamedTypeSymbol>(() => compilation.GetTypeByMetadataName(typeof(ObsoleteAttribute).FullName)!);
        }

        public INamedTypeSymbol TaskType { get => _taskType.Value; }

        public INamedTypeSymbol TaskOfTType { get => _taskOfTType.Value; }

        public INamedTypeSymbol ValueTaskType { get => _valueTaskType.Value; }

        public INamedTypeSymbol ValueTaskOfTTypeOpt { get => _valueTaskOfTTypeOpt.Value; }

        public INamedTypeSymbol IEnumerable { get => _iEnumerable.Value; }

        public INamedTypeSymbol IEnumerableGeneric { get => _iEnumerableGeneric.Value; } // IEnumerable<T>

        public INamedTypeSymbol IEnumerableOfKeyValuePair { get => _iEnumerableOfKeyValuePair.Value; }

        public INamedTypeSymbol StringType { get => _stringType.Value; }

        public INamedTypeSymbol ByteArray { get => _byteArray.Value; }

        public INamedTypeSymbol ByteType { get => _byteType.Value; }

        public INamedTypeSymbol VoidType { get => _voidType.Value; }

        public INamedTypeSymbol ReadOnlyMemoryOfBytes { get => _readOnlyMemoryOfBytes.Value; }

        public INamedTypeSymbol LookupGeneric { get => _lookupGeneric.Value; }

        public INamedTypeSymbol DictionaryGeneric { get => _dictionaryGeneric.Value; }

        public INamedTypeSymbol ObsoleteAttr { get => _obsoleteAttr.Value; }
    }
}
