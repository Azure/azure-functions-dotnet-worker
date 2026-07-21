// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using NuGet.Common;

namespace Azure.Functions.Sdk;

public sealed partial class WebJobsReference
{
    private const string ExtensionsBinaryDirectoryPath = $@"./{Constants.ExtensionsOutputFolder}";
    private const string WebJobsStartupAttributeType = "Microsoft.Azure.WebJobs.Hosting.WebJobsStartupAttribute";

    /// <summary>
    /// Gets any WebJobs references from the specified assembly metadata.
    /// </summary>
    /// <param name="reader">The metadata reader for the assembly to scan.</param>
    /// <param name="assemblyPath">The disk path of the assembly being scanned.</param>
    /// <param name="logger">The optional logger.</param>
    /// <returns>The found WebJobs references, if any.</returns>
    public static IReadOnlyList<WebJobsReference> FromModule(
        MetadataReader reader, string assemblyPath, ILogger? logger = null)
    {
        Throw.IfNull(reader);
        Throw.IfNullOrEmpty(assemblyPath);
        logger ??= NullLogger.Instance;

        List<WebJobsReference> references = [];
        string? assemblyFullName = null;
        string fileName = Path.GetFileName(assemblyPath);

        foreach (CustomAttributeHandle handle in reader.GetAssemblyDefinition().GetCustomAttributes())
        {
            CustomAttribute attribute = reader.GetCustomAttribute(handle);
            if (!IsWebJobsStartupAttribute(reader, attribute, logger))
            {
                continue;
            }

            ImmutableArray<CustomAttributeTypedArgument<string>> arguments =
                ReflectionExtensions.DecodeArguments(attribute).FixedArguments;

            // WebJobsStartupAttribute ctor args: (Type startupType, string name). The name is optional
            // and materialized as an empty string by the compiler when omitted.
            string startupTypeName = arguments.Length > 0 ? arguments[0].Value as string ?? string.Empty : string.Empty;
            string name = arguments.Length > 1 ? arguments[1].Value as string ?? string.Empty : string.Empty;

            name = GetName(name, startupTypeName);
            assemblyFullName ??= reader.GetAssemblyDefinition().GetAssemblyName().FullName;
            string assemblyQualifiedName = BuildAssemblyQualifiedName(startupTypeName, assemblyFullName);
            string hintPath = $@"{ExtensionsBinaryDirectoryPath}/{fileName}";
            references.Add(new WebJobsReference(name, assemblyQualifiedName, hintPath));
        }

        return references;
    }

    private static bool IsWebJobsStartupAttribute(MetadataReader reader, CustomAttribute attribute, ILogger logger)
    {
        try
        {
            // Match by metadata type name, walking the attribute's base-type chain only as far as types
            // defined in the scanned assembly. This never resolves the (intentionally absent) assembly
            // that defines WebJobsStartupAttribute, mirroring the previous Cecil-based behavior.
            return reader.AttributeInheritsFrom(
                attribute, WebJobsStartupAttributeType, StringComparison.OrdinalIgnoreCase);
        }
        catch (BadImageFormatException ex)
        {
            logger.LogDebug(
                "Error checking a custom attribute because its metadata could not be read."
                + $" Exception message: {ex.Message}");
            return false;
        }
    }

    // The startup type argument is serialized by name. When the type lives in the scanned assembly
    // (the normal case) the name is unqualified, so we qualify it with the scanned assembly's full name.
    // When a cross-assembly typeof was used, the serialized name already carries the assembly qualifier.
    private static string BuildAssemblyQualifiedName(string startupTypeName, string assemblyFullName)
    {
        return startupTypeName.Contains(',')
            ? startupTypeName
            : Assembly.CreateQualifiedName(assemblyFullName, startupTypeName);
    }

    // Copying the WebJobsStartup constructor logic from:
    // https://github.com/Azure/azure-webjobs-sdk/blob/e5417775bcb8c8d3d53698932ca8e4e265eac66d/src/Microsoft.Azure.WebJobs.Host/Hosting/WebJobsStartupAttribute.cs#L33-L47.
    private static string GetName(string name, string startupTypeName)
    {
        if (string.IsNullOrEmpty(name))
        {
            // for a startup class named 'CustomConfigWebJobsStartup' or 'CustomConfigStartup',
            // default to a name 'CustomConfig'
            name = GetSimpleTypeName(startupTypeName);
            int idx = name.IndexOf("WebJobsStartup");
            if (idx < 0)
            {
                idx = name.IndexOf("Startup");
            }
            if (idx > 0)
            {
                name = name[..idx];
            }
        }

        return name;
    }

    private static string GetSimpleTypeName(string typeName)
    {
        int comma = typeName.IndexOf(',');
        if (comma >= 0)
        {
            typeName = typeName[..comma];
        }

        int separator = typeName.LastIndexOfAny(['.', '+']);
        return separator >= 0 ? typeName[(separator + 1)..] : typeName;
    }
}
