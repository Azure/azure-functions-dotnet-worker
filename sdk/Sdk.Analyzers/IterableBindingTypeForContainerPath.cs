// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.Azure.Functions.Worker.Sdk.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class IterableBindingTypeForContainerPath : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(DiagnosticDescriptors.IterableBindingTypeForContainer);

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
            var d = parameter.Type;

            foreach (var attribute in parameter.GetAttributes())
            {
                var attributeType = attribute?.AttributeClass;
                if (!IsBlobInputBinding(attributeType))
                {
                    continue;
                }

                var ConstructorArguments = attribute.ConstructorArguments;

                foreach (var a in ConstructorArguments)
                {
                    if (a.Type.Name == typeof(string).Name)
                    {
                        var b = a.Value.ToString().Split('/');
                        if (b.Length < 2)
                        {
                            if (!IsIterableType(d, context))
                            {
                                var diagnostic = Diagnostic.Create(DiagnosticDescriptors.IterableBindingTypeForContainer, parameter.Locations.First(), d);
                                context.ReportDiagnostic(diagnostic);
                            }
                        }
                    }
                }
            }
        }

        static bool IsIterableType(ITypeSymbol t, SymbolAnalysisContext context)
        {
            var a = IsOrImplementsOrDerivesFrom(t, context.Compilation.GetTypeByMetadataName(typeof(IEnumerable<>).FullName)!);

            var b = IsOrImplementsOrDerivesFrom(t, context.Compilation.GetTypeByMetadataName(typeof(IEnumerable).FullName)!);

            var c = t is IArrayTypeSymbol && !SymbolEqualityComparer.Default.Equals(t, context.Compilation.GetTypeByMetadataName(typeof(byte[]).FullName)!);

            return a || b || c;
        }

        internal static bool IsOrImplementsOrDerivesFrom(ITypeSymbol symbol, ITypeSymbol? other)
        {
            return IsOrImplements(symbol, other) || IsOrDerivedFrom(symbol, other);
        }

        internal static bool IsOrDerivedFrom(ITypeSymbol symbol, ITypeSymbol? other)
        {
            if (other is null)
            {
                return false;
            }

            var current = symbol;

            while (current != null)
            {
                if (SymbolEqualityComparer.Default.Equals(current, other) || SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, other))
                {
                    return true;
                }

                current = current.BaseType;
            }

            return false;
        }

        internal static bool IsOrImplements(ITypeSymbol symbol, ITypeSymbol? other)
        {
            if (other is null)
            {
                return false;
            }

            if (symbol.Name == typeof(string).Name)
            {
                return false;
            }

            var current = symbol;

            while (current != null)
            {
                foreach (var member in current.Interfaces)
                {
                    if (IsOrDerivedFrom(member, other))
                    {
                        return true;
                    }
                }

                current = current.BaseType;
            }

            return false;
        }


        internal static bool IsBlobInputBinding(INamedTypeSymbol symbol)
        {
            var baseType = symbol.ToDisplayString();

            if (string.Equals(baseType, "Microsoft.Azure.Functions.Worker.BlobInputAttribute", StringComparison.Ordinal)
                || string.Equals(baseType, "Microsoft.Azure.WebJobs.BlobAttribute", StringComparison.Ordinal))
            {
                return true;
            }

            return false;
        }
    }
}
