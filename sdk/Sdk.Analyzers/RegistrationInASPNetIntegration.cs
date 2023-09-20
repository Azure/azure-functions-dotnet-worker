// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.Azure.Functions.Worker.Sdk.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RegistrationInASPNetIntegration : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(DiagnosticDescriptors.CorrectRegistrationExpectedInASPNetIntegration);

        private const string ExpectedRegistrationMethod = "ConfigureFunctionsWebApplication";
        private const string AspNetExtensionAssemblyName = "Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore";

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
        }

        private static void AnalyzeMethod(SymbolAnalysisContext context)
        {
            bool isASPNetAssembly = context.Compilation.ReferencedAssemblyNames.Where(x => x.Name.Equals(AspNetExtensionAssemblyName)).Any();
            string code = context.Symbol.DeclaringSyntaxReferences.First().SyntaxTree.GetText().ToString();

            if (isASPNetAssembly)
            {
                if (code.Contains(ExpectedRegistrationMethod))
                {
                    var diagnostic = Diagnostic.Create(DiagnosticDescriptors.CorrectRegistrationExpectedInASPNetIntegration, null, ExpectedRegistrationMethod);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
