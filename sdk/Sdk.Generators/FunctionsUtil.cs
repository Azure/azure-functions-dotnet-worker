﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System.Linq;

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
            MethodDeclarationSyntax method, 
            out string? functionName)
        {
            functionName = null;
            var methodSymbol = model.GetDeclaredSymbol(method);

            if (methodSymbol is null)
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.SymbolNotFound, method.Identifier.GetLocation(), nameof(methodSymbol)));
                return false;
            }

            foreach (var attr in methodSymbol.GetAttributes())
            {
                if (attr.AttributeClass != null &&
                   SymbolEqualityComparer.Default.Equals(attr.AttributeClass, compilation.GetTypeByMetadataName(Constants.Types.FunctionName)))
                {
                    functionName = (string)attr.ConstructorArguments.First().Value!; // If this is a function attribute this won't be null
                    return true;
                }
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
    }
}
