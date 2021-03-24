// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Text.RegularExpressions;

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

                // TODO: Worth checking if assembly if also present in there?
                if (string.IsNullOrEmpty(extension.HintPath) && !string.IsNullOrEmpty(assemblyName))
                {
                    extension.HintPath = $@"{ExtensionsBinaryDirectoryPath}/{assemblyName}.dll";
                }
            }
        }

        private static string? GetAssemblyNameOrNull(string? typeName)
        {
            if (typeName == null)
            {
                return null;
            }

            var match = Regex.Match(typeName, AssemblyNameFromQualifiedNameRegex);

            if (match.Success && match.Groups.Count == 2)
            {
                return match.Groups[1].Value;
            }

            return null;
        }
    }
}
