// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Microsoft.Azure.Functions.Worker.Sdk.Analyzers
{
    internal static class DiagnosticDescriptors
    {
        private static DiagnosticDescriptor Create(string id, string title,string messageFormat, string category, DiagnosticSeverity severity)
        {
            string helpLink = $"https://aka.ms/azfw-rules?ruleid={id}";
            return new DiagnosticDescriptor(id, title, messageFormat, category, severity, isEnabledByDefault: true, helpLinkUri: helpLink);
        }

        public static DiagnosticDescriptor WebJobsAttributesAreNotSupported { get; }
            = Create(id: "AZFW0001", title: "Invalid binding attributes", messageFormat: "The attribute '{0}' is a WebJobs attribute and not supported in the .NET Worker (Isolated Process).",
                category: Constants.DiagnosticsCategories.Usage, severity: DiagnosticSeverity.Error);

        public static DiagnosticDescriptor AsyncVoidReturnType { get; }
            = Create(id: "AZFW0002", title: "Avoid async void methods", messageFormat: "Do not use void as the return type for async methods. Use Task instead.",
                category: Constants.DiagnosticsCategories.Usage, severity: DiagnosticSeverity.Error);

        public static DiagnosticDescriptor DeferredBindingAttributeNotSupported{ get; }
            = Create(id: "AZFW0009", title: "Invalid class attribute", messageFormat: "The attribute '{0}' can only be used on trigger and input binding attributes.",
                category: Constants.DiagnosticsCategories.Usage, severity: DiagnosticSeverity.Error);

        public static DiagnosticDescriptor BindingTypeNotSupported{ get; }
            = Create(id: "AZFW0010", title: "Invalid binding type", messageFormat: "The binding type '{0}' is not supported by '{1}'.",
                category: Constants.DiagnosticsCategories.Usage, severity: DiagnosticSeverity.Warning);

        public static DiagnosticDescriptor IterableBindingTypeExpectedForBlobContainer { get; }
            = Create(id: "AZFW0011", title: "Invalid binding type", messageFormat: "The binding type '{0}' must be iterable for container path.",
                category: Constants.DiagnosticsCategories.Usage, severity: DiagnosticSeverity.Error);
        
        public static DiagnosticDescriptor LocalSettingsJsonNotAllowedAsConfiguration { get; }
            = Create(id: "AZFW0017", title: "local.settings.json should not be used as a configuration file", messageFormat: "There is no need to use local.settings.json as a configuration file. During development, it's automatically loaded by Functions Core Tools; in production scenarios, configuration should be handled via App Settings in Azure.",
                category: Constants.DiagnosticsCategories.Usage, severity: DiagnosticSeverity.Warning);
    }
}
