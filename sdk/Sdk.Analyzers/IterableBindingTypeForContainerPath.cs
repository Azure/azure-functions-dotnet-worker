// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.Azure.Functions.Worker.Sdk.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class IterableBindingTypeForContainerPath : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(DiagnosticDescriptors.DeferredBindingAttributeNotSupported);

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
                if (!attributeType.IsInputOrTriggerBinding())
                {
                    continue;
                }

                var ConstructorArguments = attribute.ConstructorArguments; //GetInputConverterAttributes(context.Compilation);

                foreach (var a in ConstructorArguments)
                {
                    if (a.Type.Name == typeof(string).Name)
                    {
                        var b = a.Value.ToString().Split('/');
                        if (b.Length == 1)
                        {
                            if (!IsIterableType(d, context))
                            {
                                var location = Location.Create(attribute.ApplicationSyntaxReference.SyntaxTree, attribute.ApplicationSyntaxReference.Span);
                                var diagnostic = Diagnostic.Create(DiagnosticDescriptors.DeferredBindingAttributeNotSupported, location, attribute.AttributeClass.Name);
                                context.ReportDiagnostic(diagnostic);
                            }

                        }
                    }
                }

                /*

                var allowConverterFallbackParameterValue = GetAllowConverterFallbackParameterValue(context, attributeType);
                if (allowConverterFallbackParameterValue is bool allowFallback && allowFallback)
                {
                    // If allowConverterFallback is true, we don't need to check for supported types
                    // because we don't know all of the types that are supported via the fallback
                    continue;
                }

                var supportedTypes = GetSupportedTypes(context, inputConverterAttributes);
                if (supportedTypes.Count <= 0 || supportedTypes.Contains(parameter.Type))
                {
                    continue;
                }

                ReportDiagnostic(context, parameter, attributeType);
                */
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
    }
}
