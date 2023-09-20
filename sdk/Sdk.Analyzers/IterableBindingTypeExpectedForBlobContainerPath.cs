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
    public class IterableBindingTypeExpectedForBlobContainerPath : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(DiagnosticDescriptors.IterableBindingTypeExpectedForBlobContainer);

        private const string BlobInputBindingAttribute = "Microsoft.Azure.Functions.Worker.BlobInputAttribute";
        private const string BlobContainerClientType = "Azure.Storage.Blobs.BlobContainerClient";

        private static readonly Regex BlobPathUsesExpression = new Regex("{.*}");

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
        }

        private static void AnalyzeMethod(SymbolAnalysisContext context)
        {
            // if not function then using Microsoft.Extensions.Hosting
            IEnumerable<AssemblyIdentity> e = context.Compilation.ReferencedAssemblyNames.Where(x => x.Name.Equals("Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore"));
            string code = context.Symbol.DeclaringSyntaxReferences.First().SyntaxTree.GetText().ToString();

            if (e.Any())
            {
                if (code.Contains("ConfigureFunctionsWebApplication"))
                {
                    var diagnostic = Diagnostic.Create(DiagnosticDescriptors.IterableBindingTypeExpectedForBlobContainer, null, "d");
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private static void AnalyzeParameter(SymbolAnalysisContext context, IParameterSymbol parameter)
        {
            ITypeSymbol parameterType = parameter.Type;

            foreach (AttributeData attribute in parameter.GetAttributes())
            {
                var attributeType = attribute?.AttributeClass;

                if (!IsBlobInputBinding(attributeType))
                {
                    continue;
                }

                foreach (var arg in attribute.ConstructorArguments)
                {
                    if (arg.Type.Name == typeof(string).Name)
                    {
                        string path = arg.Value.ToString();

                        // Skip this analyzer if an expression is used for the blob path
                        if (BlobPathUsesExpression.IsMatch(path))
                        {
                            continue;
                        }

                        if (path.Split('/').Length < 2
                            && !parameterType.IsIterableType(context)
                            && !string.Equals(parameterType.ToDisplayString(), BlobContainerClientType, StringComparison.Ordinal))
                        {
                            var diagnostic = Diagnostic.Create(DiagnosticDescriptors.IterableBindingTypeExpectedForBlobContainer, parameter.Locations.First(), parameterType);
                            context.ReportDiagnostic(diagnostic);
                        }
                    }
                }
            }
        }

        internal static bool IsBlobInputBinding(INamedTypeSymbol symbol)
        {
            var baseType = symbol.ToDisplayString();

            if (string.Equals(baseType, BlobInputBindingAttribute, StringComparison.Ordinal))
            {
                return true;
            }

            return false;
        }
    }
}
