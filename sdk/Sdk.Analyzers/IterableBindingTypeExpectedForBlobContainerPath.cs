// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
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

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
        }

        private static void AnalyzeMethod(SymbolAnalysisContext context)
        {
            var method = (IMethodSymbol)context.Symbol;

            if (!method.IsFunction(context))
            {
                return;
            }

            var methodParameters = method.Parameters;

            if (method.Parameters.Length <= 0)
            {
                return;
            }

            foreach (var parameter in methodParameters)
            {
                AnalyzeParameter(context, parameter);
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
