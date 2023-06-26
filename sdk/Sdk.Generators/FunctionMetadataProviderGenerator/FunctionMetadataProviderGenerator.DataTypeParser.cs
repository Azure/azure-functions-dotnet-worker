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

            /// <summary>
            /// Provides support for parsing and classifying <see cref="ITypeSymbol"/> into data types used in function metadata generation.
            /// </summary>
            /// <param name="knownTypes">A collection of known types to use for symbol comparison.</param>
            public DataTypeParser(KnownTypes knownTypes)
            {
                _knownTypes = knownTypes;
            }

            /// <summary>
            /// Get the <see cref="DataType"/> of a <see cref="ITypeSymbol"/>
            /// </summary>
            /// <param name="symbol">The <see cref="ITypeSymbol" to parse./></param>
            /// <returns>The <see cref="DataType"/> that best matches the symbol.</returns>
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

            /// <summary>
            /// Checks if a symbol is a string type or derives from a string type.
            /// </summary>
            /// <param name="symbol">The <see cref="ITypeSymbol" to parse</param>
            /// <returns>Returns true if symbol derives from a string type, else returns false.</returns>
            public bool IsStringType(ITypeSymbol symbol)
            {
                return SymbolEqualityComparer.Default.Equals(symbol, _knownTypes.StringType)
                    || (symbol is IArrayTypeSymbol arraySymbol && SymbolEqualityComparer.Default.Equals(arraySymbol.ElementType, _knownTypes.StringType));
            }

            /// <summary>
            /// Checks if a symbol is a binry type or derives from a binary type.
            /// </summary>
            /// <param name="symbol">The <see cref="ITypeSymbol" to parse</param>
            /// <returns>Returns true if symbol derives from a binary type, else returns false.</returns>
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
