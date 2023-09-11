// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Data;
using System;

namespace Microsoft.Azure.Functions.Worker.Sdk.Generators
{
    internal static class FunctionsUtil
    {
        /// <summary>
        /// Checks if a candidate method has a Function attribute on it.
        /// </summary>
        internal static bool IsValidFunctionMethod(
            GeneratorExecutionContext context,
            Compilation compilation,
            SemanticModel model, 
            MethodDeclarationSyntax method)
        {
            var methodSymbol = model.GetDeclaredSymbol(method);

            if (methodSymbol is null)
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.SymbolNotFound, method.Identifier.GetLocation(), nameof(methodSymbol)));
                return false;
            }

            if (IsFunctionSymbol(methodSymbol, compilation))
            {
                return true;
            }

            return false;
        }

        internal static bool IsFunctionSymbol(ISymbol symbol, Compilation compilation)
        {
            foreach (var attr in symbol.GetAttributes())
            {
                if (attr.AttributeClass != null &&
                   SymbolEqualityComparer.Default.Equals(attr.AttributeClass, compilation.GetTypeByMetadataName(Constants.Types.FunctionName)))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool TryGetFunctionName(ISymbol symbol, Compilation compilation, out string? functionName) 
        {
            functionName = null;

            var functionAttribute = symbol.GetAttributes().Where(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, compilation.GetTypeByMetadataName(Constants.Types.FunctionName)));
            
            if (functionAttribute.Count() > 0)
            {
                functionName = (string)functionAttribute.FirstOrDefault().ConstructorArguments.First().Value!; // If this is a function attribute this won't be null
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the fully qualified name of the method.
        /// Ex: "MyNamespaceName.MyClassName.MyMethod" 
        /// for a method called "MyMethod" inside the "MyClassName" type which is inside the "MyNamespaceName" namespace.
        /// </summary>
        internal static string GetFullyQualifiedMethodName(MethodDeclarationSyntax method, SemanticModel semanticModel)
        {
            var methodSymbol = semanticModel.GetDeclaredSymbol(method)!;
            var fullyQualifiedClassName = methodSymbol.ContainingSymbol.ToDisplayString();

            return $"{fullyQualifiedClassName}.{method.Identifier.ValueText}";
        }

        internal static string GetFullyQualifiedMethodName(IMethodSymbol method)
        {
            var fullyQualifiedClassName = method.ContainingSymbol.ToDisplayString();
            return $"{fullyQualifiedClassName}.{method.Name}";
        }

        internal static string GetFullyQualifiedMetadataName(IMethodSymbol method)
        {
            throw new NotImplementedException();
        }
    }
}
