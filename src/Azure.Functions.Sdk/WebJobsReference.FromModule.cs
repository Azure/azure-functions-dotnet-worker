// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Reflection;
using Mono.Cecil;
using NuGet.Common;

namespace Azure.Functions.Sdk;

public partial class WebJobsReference
{
    private const string ExtensionsBinaryDirectoryPath = $@"./{Constants.ExtensionsOutputFolder}";
    private const string WebJobsStartupAttributeType = "Microsoft.Azure.WebJobs.Hosting.WebJobsStartupAttribute";

    /// <summary>
    /// Gets the root path of the Azure Functions SDK module.
    /// </summary>
    public static IEnumerable<WebJobsReference> FromModule(string path, ILogger? logger = null)
    {
        Throw.IfNullOrEmpty(path);
        FunctionsAssemblyResolver resolver = new();
        ReaderParameters readerParameters = new() { AssemblyResolver = resolver };
        AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(path, readerParameters);
        return FromModule(assembly, logger);
    }

    /// <summary>
    /// Gets the root path of the Azure Functions SDK module.
    /// </summary>
    public static IEnumerable<WebJobsReference> FromModule(AssemblyDefinition assembly, ILogger? logger = null)
    {
        Throw.IfNull(assembly);
        logger ??= NullLogger.Instance;

        IEnumerable<CustomAttribute> startupAttributes = assembly.Modules
            .SelectMany(p => p.GetCustomAttributes())
            .Where(a => IsWebJobsStartupAttributeType(a.AttributeType, logger));

        foreach (CustomAttribute attribute in startupAttributes)
        {
            attribute.GetArguments(out TypeDefinition typeDef, out string name);
            name = GetName(name, typeDef);
            string assemblyQualifiedName = Assembly.CreateQualifiedName(
                typeDef.Module.Assembly.FullName, typeDef.GetReflectionFullName());
            string fileName = Path.GetFileName(assembly.MainModule.FileName);
            string hintPath = $@"{ExtensionsBinaryDirectoryPath}/{fileName}";
            yield return new WebJobsReference(name, assemblyQualifiedName, hintPath);
        }
    }

    private static bool IsWebJobsStartupAttributeType(TypeReference attributeType, ILogger logger)
    {
        try
        {
            return attributeType.CheckTypeInheritance(
                type => string.Equals(type.FullName, WebJobsStartupAttributeType, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex) when (ex is FileNotFoundException or BadImageFormatException)
        {
            string typeName = attributeType.GetReflectionFullName();
            string fileName = Path.GetFileName(attributeType.Module.FileName);
            logger.LogDebug(
                $"Error checking type inheritance for the attribute type '{typeName}' used in the assembly"
                + $" '{fileName}' because the assembly defining its base type could not be found or was invalid."
                + $" Exception message: {ex.Message}");
            return false;
        }
    }

    // Copying the WebJobsStartup constructor logic from:
    // https://github.com/Azure/azure-webjobs-sdk/blob/e5417775bcb8c8d3d53698932ca8e4e265eac66d/src/Microsoft.Azure.WebJobs.Host/Hosting/WebJobsStartupAttribute.cs#L33-L47.
    private static string GetName(string name, TypeDefinition startupTypeDef)
    {
        if (string.IsNullOrEmpty(name))
        {
            // for a startup class named 'CustomConfigWebJobsStartup' or 'CustomConfigStartup',
            // default to a name 'CustomConfig'
            name = startupTypeDef.Name;
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
