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

        public static DiagnosticDescriptor MultipleBindingsGroupedTogether { get; }
                = Create(id: "AZFW0005",
                    title: "Multiple bindings are grouped together on one property, method, or parameter syntax.",
                    messageFormat: "'{0}' must have only one binding attribute.",
                    category: "FunctionMetadataGeneration",
                    severity: DiagnosticSeverity.Error);

        public static DiagnosticDescriptor SymbolNotFound { get; }
                = Create(id: "AZFW0006",
                    title: "Symbol could not be found in user compilation.",
                    messageFormat: "The symbol '{0}' could not be found.",
                    category: "FunctionMetadataGeneration",
                    severity: DiagnosticSeverity.Warning);

        public static DiagnosticDescriptor MultipleHttpResponseTypes { get; }
                  = Create(id: "AZFW0007",
                    title: "Symbol could not be found in user compilation.",
                    messageFormat: "Found multiple public properties with type '{0}' defined in output type '{1}'.Only one HTTP response binding type is supported in your return type definition.",
                    category: "FunctionMetadataGeneration",
                    severity: DiagnosticSeverity.Error);

        public static DiagnosticDescriptor InvalidEventHubsTrigger { get; }
                  = Create(id: "AZFW0008",
                    title: "EventHub Trigger invalid.",
                    messageFormat: "The EventHub trigger on parameter '{0}' is invalid. IsBatched may be used incorrectly.",
                    category: "FunctionMetadataGeneration",
                    severity: DiagnosticSeverity.Error);
    }
}
