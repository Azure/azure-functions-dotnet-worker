// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Mono.Cecil;
using NuGet.Common;

namespace Azure.Functions.Sdk;

public static class MonoExtensions
{
    public static void GetArguments<TFirst, TSecond>(
        this CustomAttribute attribute, out TFirst first, out TSecond second)
    {
        Throw.IfNull(attribute);
        Throw.IfLessThan(attribute.ConstructorArguments.Count, 2,
            "Expected at least two constructor arguments for the attribute.");

        first = (TFirst)attribute.ConstructorArguments[0].Value;
        second = (TSecond)attribute.ConstructorArguments[1].Value;
    }

    public static bool CheckTypeInheritance(
        this TypeReference type, Func<TypeReference, bool> check, ILogger? logger = null)
    {
        Throw.IfNull(type);
        Throw.IfNull(check);

        logger ??= NullLogger.Instance;
        TypeReference? currentType = type;
        while (currentType != null)
        {
            if (check(currentType))
            {
                return true;
            }

            try
            {
                currentType = currentType.Resolve()?.BaseType;
            }
            catch (FileNotFoundException ex)
            {
                // Don't log this as an error. This most likely means it is a runtime artifact, and we
                // don't need to check those. They will never have the types we care about.
                string typeName = type.GetReflectionFullName();
                string fileName = Path.GetFileName(type.Module.FileName);
                logger.LogDebug(
                    $"Error walking type hierarchy for the attribute type '{typeName}' used in the assembly"
                    + $" '{fileName}' because the assembly defining its base type could not be found. Exception"
                    + $" message: {ex.Message}");
                return false;
            }
        }

        return false;
    }

    public static string GetReflectionFullName(this TypeReference typeRef)
    {
        Throw.IfNull(typeRef);
        return typeRef.FullName.Replace("/", "+");
    }
}
