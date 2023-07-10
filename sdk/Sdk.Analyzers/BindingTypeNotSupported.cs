// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.Azure.Functions.Worker.Sdk.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class BindingTypeNotSupported : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(DiagnosticDescriptors.BindingTypeNotSupported);

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
            foreach (var attribute in parameter.GetAttributes())
            {
                var attributeType = attribute?.AttributeClass;
                if (!attributeType.IsInputOrTriggerBinding())
                {
                    continue;
                }

                var inputConverterAttributes = attributeType.GetInputConverterAttributes(context.Compilation);
                if (inputConverterAttributes.Count <= 0)
                {
                    continue;
                }

                var converterFallbackBehaviorParameterValue = GetConverterFallbackBehaviorParameterValue(context, attributeType);
                if (converterFallbackBehaviorParameterValue.ToString() is not "Disallow")
                {
                    // If the ConverterFallbackBehavior is Allow or Default, we don't need to check for supported types
                    // because we don't know all of the types that are supported via the fallback
                    continue;
                }

                var supportedTypes = GetSupportedTypes(context, inputConverterAttributes);
                if (supportedTypes.Count <= 0 || supportedTypes.Contains(parameter.Type))
                {
                    continue;
                }

                ReportDiagnostic(context, parameter, attributeType);
            }
        }

        private static object GetConverterFallbackBehaviorParameterValue(SymbolAnalysisContext context, ITypeSymbol attributeType)
        {
            var converterFallbackBehaviorAttributeType = context.Compilation.GetTypeByMetadataName(Constants.Types.ConverterFallbackBehaviorAttribute);
            var converterFallbackBehaviorAttribute = attributeType.GetAttributes().FirstOrDefault(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, converterFallbackBehaviorAttributeType));
            return converterFallbackBehaviorAttribute.ConstructorArguments.FirstOrDefault().Value;
        }

        private static List<object> GetSupportedTypes(SymbolAnalysisContext context, List<AttributeData> inputConverterAttributes)
        {
            var supportedTypes = new List<object>();

            foreach (var inputConverterAttribute in inputConverterAttributes)
            {
                var converterName = inputConverterAttribute.ConstructorArguments.FirstOrDefault().Value.ToString();
                var converter = context.Compilation.GetTypeByMetadataName(converterName);

                var converterAttributes = converter.GetAttributes();

                var supportedTargetTypeAttributeType = context.Compilation.GetTypeByMetadataName(Constants.Types.SupportedTargetTypeAttribute);
                var converterHasSupportedTypeAttribute = converterAttributes.Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, supportedTargetTypeAttributeType));
                if (!converterHasSupportedTypeAttribute)
                {
                    // If a converter does not have the `SupportedTargetTypeAttribute`, we don't need to check for supported types
                    continue;
                }

                supportedTypes.AddRange(converterAttributes
                    .Where(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, supportedTargetTypeAttributeType))
                    .SelectMany(a => a.ConstructorArguments.Select(arg => arg.Value))
                    .ToList());
            }

            return supportedTypes;
        }

        private static void ReportDiagnostic(SymbolAnalysisContext context, IParameterSymbol parameter, ITypeSymbol attributeType)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.BindingTypeNotSupported,
                parameter.Locations.First(),
                parameter.Type.Name,
                attributeType.Name);

            context.ReportDiagnostic(diagnostic);
        }
    }
}
