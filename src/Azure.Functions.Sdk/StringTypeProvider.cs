// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Reflection.Metadata;

namespace Azure.Functions.Sdk;

/// <summary>
/// An <see cref="ICustomAttributeTypeProvider{TType}"/> that represents every type as its string
/// name and never resolves an external assembly. This lets us decode custom-attribute constructor
/// arguments (including <see cref="Type"/> arguments) purely from metadata.
/// </summary>
/// <remarks>
/// <see cref="GetSystemType"/> and <see cref="IsSystemType"/> must agree on the representation of
/// <see cref="Type"/> arguments. If they disagree, <see cref="CustomAttributeExtensions"/> decoding
/// misreads a <see cref="Type"/> argument as an enum and over-reads the blob, throwing
/// "Read out of bounds".
/// </remarks>
internal sealed class StringTypeProvider : ICustomAttributeTypeProvider<string>
{
    private const string SystemTypeName = "System.Type";

    public static StringTypeProvider Instance { get; } = new();

    public string GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode.ToString();

    public string GetSystemType() => SystemTypeName;

    public string GetSZArrayType(string elementType) => elementType + "[]";

    public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
    {
        TypeDefinition typeDef = reader.GetTypeDefinition(handle);
        return Combine(reader.GetString(typeDef.Namespace), reader.GetString(typeDef.Name));
    }

    public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
    {
        TypeReference typeRef = reader.GetTypeReference(handle);
        return Combine(reader.GetString(typeRef.Namespace), reader.GetString(typeRef.Name));
    }

    public string GetTypeFromSerializedName(string name) => name;

    public PrimitiveTypeCode GetUnderlyingEnumType(string type) => PrimitiveTypeCode.Int32;

    public bool IsSystemType(string type) => string.Equals(type, SystemTypeName, StringComparison.Ordinal);

    private static string Combine(string @namespace, string name)
    {
        return string.IsNullOrEmpty(@namespace) ? name : $"{@namespace}.{name}";
    }
}
