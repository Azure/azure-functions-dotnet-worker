// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Microsoft.Azure.Functions.Worker.Sdk.Analyzers
{
    internal class DiagnosticDescriptors
    {
        private static DiagnosticDescriptor Create(string id, string title,string messageFormat, string category, DiagnosticSeverity severity)
        {
            string helpLink = $"https://aka.ms/azfw-rules?ruleid={id}";
            return new DiagnosticDescriptor(id, title, messageFormat, category, severity, isEnabledByDefault: true, helpLinkUri: helpLink);
        }

        public static DiagnosticDescriptor WebJobsAttributesAreNotSuppoted { get; }
            = Create(id: "AZFW0001", title: "Invalid binding attributes", messageFormat: "The attribute '{0}' is a WebJobs attribute and not supported in the .NET Worker (Isolated Process).",
                category: Constants.DiagnosticsCategories.Usage, severity: DiagnosticSeverity.Error);
                
        public static DiagnosticDescriptor AsyncVoidReturnType { get; }
            = Create(id: "AZFW0002", title: "Avoid async void methods", messageFormat: "Do not use void as the return type for async methods. Use Task instead.",
                category: Constants.DiagnosticsCategories.Usage, severity: DiagnosticSeverity.Error);

    }
}
