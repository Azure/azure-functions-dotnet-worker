// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Reflection;
using NuGet.Common;

namespace Azure.Functions.Sdk;

public sealed partial class WebJobsReference
{
    private const string ExtensionsBinaryDirectoryPath = $@"./{Constants.ExtensionsOutputFolder}";
    private const string WebJobsStartupAttributeType = "Microsoft.Azure.WebJobs.Hosting.WebJobsStartupAttribute";

    /// <summary>
    /// Gets any WebJobs references from the specified assembly.
    /// </summary>
    public static IEnumerable<WebJobsReference> FromModule(Assembly assembly, ILogger? logger = null)
    {
        Throw.IfNull(assembly);
        logger ??= NullLogger.Instance;

        IEnumerable<CustomAttributeData> startupAttributes = assembly.GetCustomAttributesData()
            .Where(a => IsWebJobsStartupAttributeType(a, logger));

        foreach (CustomAttributeData attribute in startupAttributes)
        {
            attribute.GetArguments(out Type typeDef, out string name);
            name = GetName(name, typeDef);
            string assemblyQualifiedName = Assembly.CreateQualifiedName(
                typeDef.Assembly.FullName, typeDef.GetReflectionFullName());
            string fileName = Path.GetFileName(assembly.Location);
            string hintPath = $@"{ExtensionsBinaryDirectoryPath}/{fileName}";
            yield return new WebJobsReference(name, assemblyQualifiedName, hintPath);
        }
    }

    private static bool IsWebJobsStartupAttributeType(CustomAttributeData attribute, ILogger logger)
    {
        try
        {
            // Accessing AttributeType (and walking its base types) forces the metadata load context to
            // resolve the assemblies defining those types. If any cannot be resolved, treat it as a
            // non-match rather than failing the scan (mirroring the previous Cecil-based behavior).
            return attribute.AttributeType.CheckTypeInheritance(
                type => string.Equals(type.FullName, WebJobsStartupAttributeType, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex) when (ex is FileNotFoundException or BadImageFormatException)
        {
            logger.LogDebug(
                "Error checking type inheritance for a custom attribute because the assembly defining its type"
                + $" or a base type could not be found or was invalid. Exception message: {ex.Message}");
            return false;
        }
    }

    // Copying the WebJobsStartup constructor logic from:
    // https://github.com/Azure/azure-webjobs-sdk/blob/e5417775bcb8c8d3d53698932ca8e4e265eac66d/src/Microsoft.Azure.WebJobs.Host/Hosting/WebJobsStartupAttribute.cs#L33-L47.
    private static string GetName(string name, Type startupType)
    {
        if (string.IsNullOrEmpty(name))
        {
            // for a startup class named 'CustomConfigWebJobsStartup' or 'CustomConfigStartup',
            // default to a name 'CustomConfig'
            name = startupType.Name;
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
}
