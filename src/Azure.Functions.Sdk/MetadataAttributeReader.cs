// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Reflection.Metadata;

namespace Azure.Functions.Sdk;

/// <summary>
/// Helpers for reading assembly-level custom attributes directly from metadata via
/// <see cref="MetadataReader"/>, without resolving the assemblies that define those attributes.
/// This mirrors the previous Cecil-based behavior: attribute types are matched by their metadata
/// type name, so extensions can be detected even when the attribute-defining assembly (e.g.
/// <c>Microsoft.Azure.WebJobs.Host</c>) is intentionally absent from the scanned payload.
/// </summary>
internal static class MetadataAttributeReader
{
    /// <summary>
    /// Gets the full name of the type that defines the given custom attribute, without resolving
    /// external assemblies. Returns <c>null</c> when the type name cannot be read from metadata.
    /// </summary>
    /// <param name="reader">The metadata reader for the scanned assembly.</param>
    /// <param name="attribute">The custom attribute to inspect.</param>
    /// <returns>The attribute type's full name, or <c>null</c>.</returns>
    public static string? GetAttributeTypeName(MetadataReader reader, CustomAttribute attribute)
    {
        return GetTypeFullName(reader, GetAttributeTypeHandle(reader, attribute.Constructor));
    }

    /// <summary>
    /// Walks the attribute type's inheritance chain by metadata name (no assembly resolution),
    /// checking whether any type in the chain matches <paramref name="targetFullName"/>.
    /// </summary>
    /// <param name="reader">The metadata reader for the scanned assembly.</param>
    /// <param name="attribute">The custom attribute to inspect.</param>
    /// <param name="targetFullName">The full name to match against.</param>
    /// <param name="comparison">The string comparison to use.</param>
    /// <returns><c>true</c> if a matching type is found; otherwise <c>false</c>.</returns>
    public static bool AttributeInheritsFrom(
        MetadataReader reader, CustomAttribute attribute, string targetFullName, StringComparison comparison)
    {
        EntityHandle typeHandle = GetAttributeTypeHandle(reader, attribute.Constructor);
        while (!typeHandle.IsNil)
        {
            string? fullName = GetTypeFullName(reader, typeHandle);
            if (fullName is not null && string.Equals(fullName, targetFullName, comparison))
            {
                return true;
            }

            // We can only follow the base-type chain for types defined in this assembly. When the
            // attribute (or a base type) is an external TypeReference, its base type lives in another
            // assembly that we deliberately do not resolve, so the walk stops here. This mirrors the
            // previous Cecil behavior where an unresolvable base type ended the walk; in practice every
            // real usage matches on the first (TypeReference) check.
            if (typeHandle.Kind != HandleKind.TypeDefinition)
            {
                break;
            }

            typeHandle = reader.GetTypeDefinition((TypeDefinitionHandle)typeHandle).BaseType;
        }

        return false;
    }

    /// <summary>
    /// Decodes the fixed constructor arguments of a custom attribute, representing every type as its
    /// string name so that no external assembly is resolved.
    /// </summary>
    /// <param name="attribute">The custom attribute to decode.</param>
    /// <returns>The decoded attribute value.</returns>
    public static CustomAttributeValue<string> DecodeArguments(CustomAttribute attribute)
    {
        return attribute.DecodeValue(StringTypeProvider.Instance);
    }

    private static EntityHandle GetAttributeTypeHandle(MetadataReader reader, EntityHandle constructor)
    {
        return constructor.Kind switch
        {
            HandleKind.MethodDefinition =>
                reader.GetMethodDefinition((MethodDefinitionHandle)constructor).GetDeclaringType(),
            HandleKind.MemberReference =>
                reader.GetMemberReference((MemberReferenceHandle)constructor).Parent,
            _ => default,
        };
    }

    private static string? GetTypeFullName(MetadataReader reader, EntityHandle typeHandle)
    {
        switch (typeHandle.Kind)
        {
            case HandleKind.TypeReference:
                TypeReference typeRef = reader.GetTypeReference((TypeReferenceHandle)typeHandle);
                return Combine(reader.GetString(typeRef.Namespace), reader.GetString(typeRef.Name));
            case HandleKind.TypeDefinition:
                TypeDefinition typeDef = reader.GetTypeDefinition((TypeDefinitionHandle)typeHandle);
                return Combine(reader.GetString(typeDef.Namespace), reader.GetString(typeDef.Name));
            default:
                return null;
        }
    }

    private static string Combine(string @namespace, string name)
    {
        return string.IsNullOrEmpty(@namespace) ? name : $"{@namespace}.{name}";
    }
}
