// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Functions.Worker.Sdk.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal partial class FunctionExecutorGenerator
{
    internal class Parser
    {
        private readonly GeneratorExecutionContext _context;

        private readonly KnownTypes _knownTypes;

        private Compilation Compilation => _context.Compilation;

        internal Parser(GeneratorExecutionContext context)
        {
            _context = context;
            _knownTypes = new KnownTypes(_context.Compilation);
        }

        internal ICollection<ExecutableFunction> GetFunctions(List<MethodDeclarationSyntax> methods)
        {
            var classDict = new Dictionary<string, FunctionClass>();
            var functionList = new List<ExecutableFunction>();

            foreach (MethodDeclarationSyntax method in methods)
            {
                _context.CancellationToken.ThrowIfCancellationRequested();
                var model = Compilation.GetSemanticModel(method.SyntaxTree);

                if (!FunctionsUtil.IsValidMethodAzureFunction(_context, Compilation, model, method,
                        out _))
                {
                    continue;
                }

                var methodName = method.Identifier.Text;
                var methodParameterList = new List<string>(method.ParameterList.Parameters.Count);

                foreach (var methodParam in method.ParameterList.Parameters)
                {
                    if (model.GetDeclaredSymbol(methodParam) is not IParameterSymbol parameterSymbol)
                    {
                        continue;
                    }

                    methodParameterList.Add(parameterSymbol.Type.ToDisplayString());
                }

                var methodSymSemanticModel = Compilation.GetSemanticModel(method.SyntaxTree);
                var methodSymbol = methodSymSemanticModel.GetDeclaredSymbol(method)!;
                var fullyQualifiedClassName = methodSymbol.ContainingSymbol.ToDisplayString();
                var functionClass = (ClassDeclarationSyntax)method.Parent!;
                var entryPoint = $"{fullyQualifiedClassName}.{methodName}";

                if (!classDict.TryGetValue(fullyQualifiedClassName, out var classInfo))
                {
                    classInfo = new FunctionClass(fullyQualifiedClassName)
                    {
                        ConstructorParameterTypeNames = GetConstructorParamTypeNames(functionClass, model)
                    };
                    classDict[fullyQualifiedClassName] = classInfo;
                }

                var funcInfo = new ExecutableFunction
                {
                    EntryPoint = entryPoint,
                    ParameterTypeNames = methodParameterList,
                    MethodName = methodName,
                    ShouldAwait = IsTaskType(methodSymbol.ReturnType),
                    IsReturnValueAssignable = IsReturnValueAssignable(methodSymbol),
                    IsStatic = method.Modifiers.Any(SyntaxKind.StaticKeyword),
                    ParentFunctionClass = classInfo
                };

                functionList.Add(funcInfo);
            }

            return functionList;
        }

        private bool IsTaskType(ITypeSymbol typeSymbol)
        {
            return
                SymbolEqualityComparer.Default.Equals(typeSymbol.OriginalDefinition, _knownTypes.TaskType) ||
                SymbolEqualityComparer.Default.Equals(typeSymbol.OriginalDefinition, _knownTypes.TaskOfTType) ||
                SymbolEqualityComparer.Default.Equals(typeSymbol.OriginalDefinition, _knownTypes.ValueTaskType) ||
                SymbolEqualityComparer.Default.Equals(typeSymbol.OriginalDefinition, _knownTypes.ValueTaskOfTTypeOpt);
        }

        private bool IsReturnValueAssignable(IMethodSymbol methodSymbol)
        {
            if (methodSymbol.ReturnsVoid)
            {
                return false;
            }

            if (SymbolEqualityComparer.Default.Equals(methodSymbol.ReturnType.OriginalDefinition, _knownTypes.TaskType))
            {
                return false;
            }

            if (SymbolEqualityComparer.Default.Equals(methodSymbol.ReturnType.OriginalDefinition,
                    _knownTypes.ValueTaskType))
            {
                return false;
            }

            return true;
        }

        private static IEnumerable<string> GetConstructorParamTypeNames(ClassDeclarationSyntax functionClass,
            SemanticModel model)
        {
            var firstConstructorMember = GetBestConstructor(functionClass);

            if (firstConstructorMember is not ConstructorDeclarationSyntax constructorSyntax)
            {
                return Enumerable.Empty<string>();
            }

            var constructorParamTypeNames = new List<string>(constructorSyntax.ParameterList.Parameters.Count);

            foreach (var param in constructorSyntax.ParameterList.Parameters)
            {
                if (model.GetDeclaredSymbol(param) is not IParameterSymbol parameterSymbol)
                {
                    continue;
                }

                // We are getting fully qualified name of the type
                constructorParamTypeNames.Add(parameterSymbol.Type.ToDisplayString());
            }

            return constructorParamTypeNames;
        }

        private static MemberDeclarationSyntax? GetBestConstructor(ClassDeclarationSyntax functionClass)
        {
            // TO DO: Fix this.
            // Currently picking first constructor.
            var firstConstructorMember =
                functionClass.Members.FirstOrDefault(member => member is ConstructorDeclarationSyntax);

            return firstConstructorMember;
        }
    }
}
