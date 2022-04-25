// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Microsoft.Azure.Functions.Worker.Sdk.Generators
{
    internal class DiagnosticDescriptors
    {
        private static DiagnosticDescriptor Create(string id, string title, string messageFormat, string category, DiagnosticSeverity severity)
        {
            var helpLink = $"https://aka.ms/azfw-rules?ruleid={id}";

            return new DiagnosticDescriptor(id, title, messageFormat, category, severity, isEnabledByDefault: true, helpLinkUri: helpLink);
        }

        public static DiagnosticDescriptor IncorrectBaseType { get; }
                = Create(id: "AZFW0003",
                    title: "Invalid base class for extension startup type.",
                    messageFormat: "'{0}' must derive from '{1}'.",
                    category: "Startup",
                    severity: DiagnosticSeverity.Error);

        public static DiagnosticDescriptor ConstructorMissing { get; }
                = Create(id: "AZFW0004",
                    title: "Extension startup type is missing parameterless constructor.",
                    messageFormat: "'{0}' class must have a public parameterless constructor.",
                    category: "Startup",
                    severity: DiagnosticSeverity.Error);
    }
}
