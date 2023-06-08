// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.;

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace Microsoft.Azure.Functions.Worker.Sdk.Generators
{
    public partial class FunctionMetadataProviderGenerator
    {
        internal sealed class DataTypeParser
        {
            private readonly KnownTypes _knownTypes;

            public DataTypeParser(KnownTypes knownTypes)
            {
                _knownTypes = knownTypes;
            }

            public DataType GetDataType(ITypeSymbol symbol)
            {
                if (IsStringType(symbol))
                {
                    return DataType.String;
                }
                // Is binary parameter type
                else if (IsBinaryType(symbol))
                {
                    return DataType.Binary;
                }

                return DataType.Undefined;
            }

            public bool IsStringType(ITypeSymbol symbol)
            {
                return SymbolEqualityComparer.Default.Equals(symbol, _knownTypes.StringType)
                    || (symbol is IArrayTypeSymbol arraySymbol && SymbolEqualityComparer.Default.Equals(arraySymbol.ElementType, _knownTypes.StringType));
            }

            public bool IsBinaryType(ITypeSymbol symbol)
            {
                var isByteArray = SymbolEqualityComparer.Default.Equals(symbol, _knownTypes.ByteArray)
                    || (symbol is IArrayTypeSymbol arraySymbol && SymbolEqualityComparer.Default.Equals(arraySymbol.ElementType, _knownTypes.ByteType));
                var isReadOnlyMemoryOfBytes = SymbolEqualityComparer.Default.Equals(symbol, _knownTypes.ReadOnlyMemoryOfBytes);
                var isArrayOfByteArrays = symbol is IArrayTypeSymbol outerArray &&
                    outerArray.ElementType is IArrayTypeSymbol innerArray && SymbolEqualityComparer.Default.Equals(innerArray.ElementType, _knownTypes.ByteType);

                return isByteArray || isReadOnlyMemoryOfBytes || isArrayOfByteArrays;
            }
        }
    }
}
