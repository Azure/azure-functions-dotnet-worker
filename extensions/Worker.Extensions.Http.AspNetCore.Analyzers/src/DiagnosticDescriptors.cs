﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore
{
    internal class DiagnosticDescriptors
    {
        public const string Usage = "Usage";

        private static DiagnosticDescriptor Create(string id, string title, string messageFormat, string category, DiagnosticSeverity severity)
        {
            var helpLink = $"https://aka.ms/azfw-rules?ruleid={id}";
            return new DiagnosticDescriptor(id, title, messageFormat, category, severity, isEnabledByDefault: true, helpLinkUri: helpLink);
        }

        public static DiagnosticDescriptor CorrectRegistrationExpectedInAspNetIntegration { get; }
            = Create(id: "AZFW0014", title: "Missing expected registration of ASP.NET Core Integration services", messageFormat: "The registration for method '{0}' is expected for ASP.NET Core Integration.",
                category: Usage, severity: DiagnosticSeverity.Error);
        public static DiagnosticDescriptor MultipleOutputHttpTriggerWithoutHttpResultAttribute { get; }
            = Create(id: "AZFW0015", title: "Missing a HttpResultAttribute in multi-output function", messageFormat: "The return type for function '{0}' is missing a HttpResultAttribute on the HTTP response type property.",
                category: Usage, severity: DiagnosticSeverity.Error);

        public static DiagnosticDescriptor MultipleOutputWithHttpResponseDataWithoutHttpResultAttribute { get; }
            = Create(id: "AZFW0016", title: "Missing a HttpResultAttribute in multi-output function", messageFormat: "The return type for function '{0}' is missing a HttpResultAttribute on the HttpResponseData type property.",
        category: Usage, severity: DiagnosticSeverity.Warning);
    }
}
