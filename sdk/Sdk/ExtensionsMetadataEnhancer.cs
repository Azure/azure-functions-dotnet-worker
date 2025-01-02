// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Mono.Cecil;

namespace Microsoft.Azure.Functions.Worker.Sdk
{
    internal class ExtensionsMetadataEnhancer
    {
        private const string ExtensionsBinaryDirectoryPath = @"./.azurefunctions";
        private const string AssemblyNameFromQualifiedNameRegex = @"^.+,+\s(.+),\sVersion=.+$";

        public static void AddHintPath(IEnumerable<ExtensionReference> extensions)
        {
            foreach (ExtensionReference extension in extensions)
            {
                string? assemblyName = GetAssemblyNameOrNull(extension.TypeName);

                if (string.IsNullOrEmpty(extension.HintPath) && !string.IsNullOrEmpty(assemblyName))
                {
                    extension.HintPath = $@"{ExtensionsBinaryDirectoryPath}/{assemblyName}.dll";
                }
            }
        }

        public static IEnumerable<ExtensionReference> GetWebJobsExtensions(string fileName)
        {
            // NOTE: this is an incomplete approach to getting extensions and is intended only for our usages.
            // Running this with arbitrary assemblies (especially user supplied) can lead to exceptions.
            AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(fileName);
            IEnumerable<CustomAttribute> attributes = assembly.Modules.SelectMany(p => p.GetCustomAttributes())
                .Where(a => a.AttributeType.FullName == "Microsoft.Azure.WebJobs.Hosting.WebJobsStartupAttribute");

            foreach (CustomAttribute attribute in attributes)
            {
                CustomAttributeArgument typeProperty = attribute.ConstructorArguments.ElementAtOrDefault(0);
                CustomAttributeArgument nameProperty = attribute.ConstructorArguments.ElementAtOrDefault(1);

                TypeDefinition typeDef = (TypeDefinition)typeProperty.Value;
                string assemblyQualifiedName = Assembly.CreateQualifiedName(
                    typeDef.Module.Assembly.FullName, GetReflectionFullName(typeDef));

                string name = GetName((string)nameProperty.Value, typeDef);

                yield return new ExtensionReference
                {
                    Name = name,
                    TypeName = assemblyQualifiedName,
                    HintPath = $@"{ExtensionsBinaryDirectoryPath}/{Path.GetFileName(fileName)}",
                };
            }
        }

        private static string? GetAssemblyNameOrNull(string? typeName)
        {
            if (typeName == null)
            {
                return null;
            }

            var match = Regex.Match(typeName, AssemblyNameFromQualifiedNameRegex);

            if (match is {Success: true, Groups.Count: 2})
            {
                return match.Groups[1].Value;
            }

            return null;
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
                    name = name.Substring(0, idx);
                }
            }

            return name;
        }

        private static string GetReflectionFullName(TypeReference typeRef)
        {
            return typeRef.FullName.Replace("/", "+");
        }
    }
}
