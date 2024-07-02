// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class HttpResultAttributeExpectedAnalyzer : DiagnosticAnalyzer
    {
        private const string FunctionAttributeFullName = "Microsoft.Azure.Functions.Worker.FunctionAttribute";
        private const string HttpTriggerAttributeFullName = "Microsoft.Azure.Functions.Worker.HttpTriggerAttribute";
        private const string HttpResultAttributeFullName = "Microsoft.Azure.Functions.Worker.HttpResultAttribute";
        public const string HttpResponseDataFullName = "Microsoft.Azure.Functions.Worker.Http.HttpResponseData";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.MultipleOutputHttpTriggerWithoutHttpResultAttribute);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
        }

        private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
        {
            var semanticModel = context.SemanticModel;
            var methodDeclaration = (MethodDeclarationSyntax) context.Node;

            var functionAttributeSymbol = semanticModel.Compilation.GetTypeByMetadataName(FunctionAttributeFullName);
            var functionNameAttribute = methodDeclaration.AttributeLists
                .SelectMany(attrList => attrList.Attributes)
                .Where(attr => SymbolEqualityComparer.Default.Equals(semanticModel.GetTypeInfo(attr).Type, functionAttributeSymbol));

            if (!functionNameAttribute.Any())
            {
                return;
            }

            var functionName = functionNameAttribute.First().ArgumentList.Arguments[0]; // only one argument in FunctionAttribute which is the function name

            var httpTriggerAttributeSymbol = semanticModel.Compilation.GetTypeByMetadataName(HttpTriggerAttributeFullName);
            var hasHttpTriggerAttribute = methodDeclaration.ParameterList.Parameters
                .SelectMany(param => param.AttributeLists)
                .SelectMany(attrList => attrList.Attributes)
                .Select(attr => semanticModel.GetTypeInfo(attr).Type)
                .Any(attrSymbol => SymbolEqualityComparer.Default.Equals(attrSymbol, httpTriggerAttributeSymbol));

            if (!hasHttpTriggerAttribute)
            {
                return;
            }

            var returnType = methodDeclaration.ReturnType;
            var returnTypeSymbol = semanticModel.GetTypeInfo(returnType).Type;

            var taskSymbol = semanticModel.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
            var isGenericTask = returnTypeSymbol != null && returnTypeSymbol.OriginalDefinition.Equals(taskSymbol, SymbolEqualityComparer.Default);
            var isTaskOfCustomType = isGenericTask && returnTypeSymbol is INamedTypeSymbol genericSymbol
                && IsCustomType(genericSymbol.TypeArguments[0]);
            var isCustomReturnType = IsCustomType(returnTypeSymbol);

            if (!(isTaskOfCustomType || isCustomReturnType))
            {
                return;
            }

            var httpResponseDataSymbol = semanticModel.Compilation.GetTypeByMetadataName(HttpResponseDataFullName);
            var hasHttpResponseData = returnTypeSymbol.GetMembers()
                .OfType<IPropertySymbol>()
                .Any(prop => SymbolEqualityComparer.Default.Equals(prop.Type, httpResponseDataSymbol));

            var httpResultAttributeSymbol = semanticModel.Compilation.GetTypeByMetadataName(HttpResultAttributeFullName);
            var hasHttpResultAttribute = returnTypeSymbol.GetMembers()
                .SelectMany(member => member.GetAttributes())
                .Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, httpResultAttributeSymbol));

            if (!hasHttpResultAttribute && !hasHttpResponseData)
            {
                var diagnostic = Diagnostic.Create(DiagnosticDescriptors.MultipleOutputHttpTriggerWithoutHttpResultAttribute, methodDeclaration.ReturnType.GetLocation(), functionName.ToString());
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static bool IsCustomType(ISymbol symbol)
        {
            return symbol != null && symbol.ContainingAssembly.ToDisplayString() != typeof(object).Assembly.ToString()
                    && symbol.ContainingAssembly.ToDisplayString() != typeof(Task).Assembly.ToString();
        }
    }
}
