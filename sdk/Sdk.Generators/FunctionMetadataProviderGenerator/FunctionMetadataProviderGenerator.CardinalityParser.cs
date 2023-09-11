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
        internal sealed class CardinalityParser
        {
            private readonly KnownTypes _knownTypes;
            private readonly KnownFunctionMetadataTypes _knownFunctionMetadataTypes;
            private DataTypeParser _dataTypeParser;

            /// <summary>
            /// Provides support for validating and parsing cardinality scenarios when generating function metadata.
            /// </summary>
            /// <param name="knownTypes">A collection of known types, used for symbol comparison.</param>
            /// <param name="knownFunctionMetadataTypes">A collection of known types from Azure Functions packages, used for symbol comparison.</param>
            /// <param name="dataTypeParser"><see cref="DataTypeParser"/> used to aid in parsing data types used in function metadata generation.</param>
            public CardinalityParser(KnownTypes knownTypes, KnownFunctionMetadataTypes knownFunctionMetadataTypes, DataTypeParser dataTypeParser)
            {
                _knownTypes = knownTypes;
                _knownFunctionMetadataTypes = knownFunctionMetadataTypes;
                _dataTypeParser = dataTypeParser;
            }

            /// <summary>
            /// Checks if an attribute has cardinality.
            /// </summary>
            /// <param name="attribute">The attribute to check.</param>
            /// <returns>Returns true if cardinality is supported, else returns false.</returns>
            public bool IsCardinalitySupported(AttributeData attribute)
            {
                return TryGetIsBatchedProp(attribute, out var isBatchedProp);
            }

            /// <summary>
            /// Checks if an attribute contains a property called "IsBatched", which indicates that cardinality is supported.
            /// </summary>
            /// <param name="attribute">The attribute to check.</param>
            /// <param name="isBatchedProp">The attribute's IsBatched property represented as an <see cref="ISymbol"/> or <see cref="null""/> if no IsBatched property is found.</param>
            /// <returns></returns>
            private bool TryGetIsBatchedProp(AttributeData attribute, out ISymbol? isBatchedProp)
            {
                var attrClass = attribute.AttributeClass;
                isBatchedProp = attrClass!
                    .GetMembers()
                    .SingleOrDefault(m => string.Equals(m.Name, Constants.FunctionMetadataBindingProps.IsBatchedKey, StringComparison.OrdinalIgnoreCase));

                return isBatchedProp != null;
            }

            /// <summary>
            /// Verifies that a binding that has Cardinality (isBatched property) is valid. If isBatched is set to true, the parameter with the
            /// attribute must be an iterable collection.
            /// </summary>
            /// <param name="parameterSymbol">The parameter associated with a binding attribute that supports cardinality represented as an <see cref="IParameterSymbol"/>.</param>
            /// <param name="attribute">The binding attribute that supports cardinality.</param>
            /// <param name="dataType">The <see cref="DataType"/> that best represents the parameter.</param>
            /// <returns>Returns true if the parameter is compatible with the cardinality defined by the attribute, else returns false.</returns>
            public bool IsCardinalityValid(IParameterSymbol parameterSymbol, AttributeData attribute, out DataType dataType)
            {
                dataType = DataType.Undefined;
                var cardinalityIsNamedArg = false;

                // check if IsBatched is defined in the NamedArguments
                foreach (var arg in attribute.NamedArguments)
                {
                    if (String.Equals(arg.Key, Constants.FunctionMetadataBindingProps.IsBatchedKey) &&
                        arg.Value.Value != null)
                    {
                        cardinalityIsNamedArg = true;
                        var isBatched = (bool)arg.Value.Value; // isBatched takes in booleans so we can just type cast it here to use

                        if (!isBatched)
                        {
                            dataType = _dataTypeParser.GetDataType(parameterSymbol.Type);
                            return true;
                        }
                    }
                }

                // When "IsBatched" is not a named arg, we have to check the default value
                if (!cardinalityIsNamedArg)
                {
                    if (!TryGetIsBatchedProp(attribute, out var isBatchedProp))
                    {
                        dataType = DataType.Undefined;
                        return false;
                    }

                    var defaultValAttr = isBatchedProp!
                        .GetAttributes()
                        .SingleOrDefault(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, _knownFunctionMetadataTypes.DefaultValue));

                    var defaultVal = defaultValAttr!.ConstructorArguments.SingleOrDefault().Value!.ToString(); // there is only one constructor arg for the DefaultValue attribute (the default value)

                    if (!bool.TryParse(defaultVal, out bool b) || !b)
                    {
                        dataType = _dataTypeParser.GetDataType(parameterSymbol.Type);
                        return true;
                    }
                }

                // we check if the param is an array type
                // we exclude byte arrays (byte[]) b/c we handle that as Cardinality.One (we handle this similar to how a char[] is basically a string)
                if (parameterSymbol.Type is IArrayTypeSymbol && !SymbolEqualityComparer.Default.Equals(parameterSymbol.Type, _knownTypes.ByteArray))
                {
                    dataType = _dataTypeParser.GetDataType(parameterSymbol.Type);
                    return true;
                }

                // Check if mapping type - mapping enumerables are not valid types for Cardinality.Many
                if (parameterSymbol.Type.IsOrImplementsOrDerivesFrom(_knownTypes.IEnumerableOfKeyValuePair)
                    || parameterSymbol.Type.IsOrImplementsOrDerivesFrom(_knownTypes.LookupGeneric)
                    || parameterSymbol.Type.IsOrImplementsOrDerivesFrom(_knownTypes.DictionaryGeneric))
                {
                    return false;
                }

                var isGenericEnumerable = parameterSymbol.Type.IsOrImplementsOrDerivesFrom(_knownTypes.IEnumerableGeneric);
                var isEnumerable = parameterSymbol.Type.IsOrImplementsOrDerivesFrom(_knownTypes.IEnumerable);

                if (!_dataTypeParser.IsStringType(parameterSymbol.Type) && (isGenericEnumerable || isEnumerable))
                {
                    if (_dataTypeParser.IsStringType(parameterSymbol.Type))
                    {
                        dataType = DataType.String;
                    }
                    else if (_dataTypeParser.IsBinaryType(parameterSymbol.Type))
                    {
                        dataType = DataType.Binary;
                    }
                    else if (isGenericEnumerable)
                    {
                        dataType = ResolveIEnumerableOfT(parameterSymbol, out bool hasError);

                        if (hasError)
                        {
                            return false;
                        }

                        return true;
                    }
                    return true;
                }

                // trigger input type doesn't match any of the valid cases so return false
                return false;
            }

            /// <summary>
            /// Find the underlying data type of an IEnumerableOfT (String, Binary, Undefined)
            /// ex. IEnumerable<byte[]> would return DataType.Binary
            /// </summary>
            private DataType ResolveIEnumerableOfT(IParameterSymbol parameterSymbol, out bool hasError)
            {
                var result = DataType.Undefined;
                hasError = false;

                var currSymbol = parameterSymbol.Type;
                INamedTypeSymbol? finalSymbol = null;

                while (currSymbol != null)
                {
                    INamedTypeSymbol? genericInterfaceSymbol = null;

                    if (currSymbol.IsOrDerivedFrom(_knownTypes.IEnumerableGeneric) && currSymbol is INamedTypeSymbol currNamedSymbol)
                    {
                        finalSymbol = currNamedSymbol;
                        break;
                    }

                    genericInterfaceSymbol = currSymbol.Interfaces.Where(i => i.IsOrDerivedFrom(_knownTypes.IEnumerableGeneric)).FirstOrDefault();
                    if (genericInterfaceSymbol != null)
                    {
                        finalSymbol = genericInterfaceSymbol;
                        break;
                    }

                    currSymbol = currSymbol.BaseType;
                }

                if (finalSymbol is null)
                {
                    hasError = true;
                    return result;
                }

                var argument = finalSymbol.TypeArguments.FirstOrDefault(); // we've already checked and discarded mapping types by this point - should be a single argument

                if (argument is null)
                {
                    hasError = true;
                    return result;
                }

                return _dataTypeParser.GetDataType(argument);
            }
        }
    }
}
