// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using System.Collections.Immutable;
using System.Threading.Tasks;
using System.Composition;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;
using System.Threading;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using System;
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

                var supportedTypes = GetSupportedTypes(context, inputConverterAttributes);
                if (supportedTypes.Count <= 0)
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

        private static List<object> GetSupportedTypes(SymbolAnalysisContext context, List<AttributeData> inputConverterAttributes)
        {
            var supportedTypes = new List<object>();

            foreach (var inputConverterAttribute in inputConverterAttributes)
            {
                var converterName = inputConverterAttribute.ConstructorArguments.FirstOrDefault().Value.ToString();
                var converter = context.Compilation.GetTypeByMetadataName(converterName);

                var converterAttributes = converter.GetAttributes();

                var converterHasSupportedTypeAttribute = converterAttributes.Any(a => a.AttributeClass.Name == Constants.Names.SupportedConverterTypeAttribute);
                if (!converterHasSupportedTypeAttribute)
                {
                    // If a converter does not have the `SupportedConverterTypeAttribute`, we don't need to check for supported types
                    continue;
                }

                supportedTypes.AddRange(converterAttributes
                    .Where(a => a.AttributeClass.Name == Constants.Names.SupportedConverterTypeAttribute)
                    .SelectMany(a => a.ConstructorArguments.Select(arg => arg.Value))
                    .ToList());
            }

            return supportedTypes;
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
