// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;

namespace Microsoft.Azure.Functions.Worker.Sdk.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class BindingTypeAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.SupportedBindingType);
        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);

            context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
        }

        private void AnalyzeMethod(SymbolAnalysisContext context)
        {
            var method = (IMethodSymbol)context.Symbol;

            if (!method.IsFunction(context))
            {
                return;
            }

            var methodParameters = method.Parameters;

            if (method.Parameters.Length == 0)
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

                var inputConverterAttributes = GetInputConverterAttributes(context, attributeType);
                if (inputConverterAttributes.Count == 0)
                {
                    continue;
                }

                var hasSupportedTypes = ConverterAdvertisesSupportedTypes(context, inputConverterAttributes);
                if (!hasSupportedTypes)
                {
                    continue;
                }

                ReportDiagnostic(context, parameter, attributeType);
            }
        }

        private static List<AttributeData> GetInputConverterAttributes(SymbolAnalysisContext context, ITypeSymbol attributeType)
        {
            var inputConverterAttributeType = context.Compilation.GetTypeByMetadataName(Constants.Types.InputConverterAttribute);
            return attributeType.GetAttributes()
                .Where(attr => attr.AttributeClass.Equals(inputConverterAttributeType))
                .ToList();
        }

        private static bool ConverterAdvertisesSupportedTypes(SymbolAnalysisContext context, List<AttributeData> inputConverterAttributes)
        {
            return inputConverterAttributes
                .Select(inputConverterAttribute =>
                {
                    var converterName = inputConverterAttribute.ConstructorArguments.FirstOrDefault().Value.ToString();
                    var converter = context.Compilation.GetTypeByMetadataName(converterName);
                    return converter.GetAttributes();
                })
                .Any(converters => converters.Any(a => a.AttributeClass.Name == Constants.Names.SupportedConverterTypeAttribute));
        }

        private static void ReportDiagnostic(SymbolAnalysisContext context, IParameterSymbol parameter, ITypeSymbol attributeType)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.SupportedBindingType,
                parameter.Locations.First(),
                attributeType.Name);

            context.ReportDiagnostic(diagnostic);
        }
    }
}
