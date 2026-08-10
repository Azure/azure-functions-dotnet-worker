// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Reflection;
using Mono.Cecil;
using NuGet.Common;

namespace Azure.Functions.Sdk;

public sealed partial class WebJobsReference
{
    private const string ExtensionsBinaryDirectoryPath = $@"./{Constants.ExtensionsOutputFolder}";
    private const string FunctionsStartupAttributeType = "Microsoft.Azure.Functions.Extensions.DependencyInjection.FunctionsStartupAttribute";
    private const string StringType = "System.String";
    private const string TypeType = "System.Type";
    private const string WebJobsStartupAttributeType = "Microsoft.Azure.WebJobs.Hosting.WebJobsStartupAttribute";

    /// <summary>
    /// Gets any WebJobs references from the specified assembly.
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
            GetStartupTypeAndName(attribute, out TypeDefinition typeDef, out string name);

            string assemblyQualifiedName = Assembly.CreateQualifiedName(
                typeDef.Module.Assembly.FullName, typeDef.GetReflectionFullName());
            string fileName = Path.GetFileName(assembly.MainModule.FileName);
            string hintPath = $@"{ExtensionsBinaryDirectoryPath}/{fileName}";
            yield return new WebJobsReference(name, assemblyQualifiedName, hintPath);
        }
    }

    private static void GetStartupTypeAndName(
        CustomAttribute attribute,
        out TypeDefinition startupType,
        out string name)
    {
        IList<CustomAttributeArgument> arguments = attribute.ConstructorArguments;
        if (arguments.Count == 1
            && string.Equals(arguments[0].Type.FullName, TypeType, StringComparison.Ordinal))
        {
            startupType = (TypeDefinition)arguments[0].Value;
            name = startupType.Name;
            return;
        }

        if (arguments.Count == 2
            && string.Equals(arguments[0].Type.FullName, TypeType, StringComparison.Ordinal)
            && string.Equals(arguments[1].Type.FullName, StringType, StringComparison.Ordinal))
        {
            startupType = (TypeDefinition)arguments[0].Value;
            name = GetName((string)arguments[1].Value, startupType);
            return;
        }

        throw new InvalidOperationException(
            $"Unexpected constructor signature for startup attribute '{attribute.AttributeType.FullName}'.");
    }

    private static bool IsWebJobsStartupAttributeType(TypeReference attributeType, ILogger logger)
    {
        try
        {
            return attributeType.CheckTypeInheritance(
                type => string.Equals(type.FullName, WebJobsStartupAttributeType, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(type.FullName, FunctionsStartupAttributeType, StringComparison.OrdinalIgnoreCase));
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
