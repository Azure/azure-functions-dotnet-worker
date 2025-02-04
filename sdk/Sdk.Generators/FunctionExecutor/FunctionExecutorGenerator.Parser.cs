// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.Azure.Functions.Worker.Sdk.Generators
{
    public partial class FunctionExecutorGenerator
    {
        internal sealed class Parser
        {
            private readonly GeneratorExecutionContext _context;
            private readonly KnownTypes _knownTypes;

            internal Parser(GeneratorExecutionContext context)
            {
                _context = context;
                _knownTypes = new KnownTypes(_context.Compilation);
            }

            private Compilation Compilation => _context.Compilation;

            internal ICollection<ExecutableFunction> GetFunctions(IEnumerable<IMethodSymbol> methods)
            {
                var functionList = new List<ExecutableFunction>();

                foreach (IMethodSymbol method in methods.Where(m=>m.DeclaredAccessibility == Accessibility.Public))
                {
                    _context.CancellationToken.ThrowIfCancellationRequested();

                    var methodName = method.Name;
                    var methodParameterList = new List<string>();

                    foreach (IParameterSymbol parameterSymbol in method.Parameters)
                    {
                        var fullyQualifiedTypeName = parameterSymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        methodParameterList.Add(fullyQualifiedTypeName);
                    }

                    var defaultFormatClassName = method.ContainingSymbol.ToDisplayString();
                    var fullyQualifiedClassName = method.ContainingSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                    var isObsolete = method.GetAttributes()
                        .Any(attr => attr.AttributeClass?.ToString() == "System.ObsoleteAttribute");
                    
                    var function = new ExecutableFunction
                    {
                        EntryPoint = $"{defaultFormatClassName}.{method.Name}",
                        ParameterTypeNames = methodParameterList,
                        MethodName = methodName,
                        ShouldAwait = IsTaskType(method.ReturnType),
                        IsReturnValueAssignable = IsReturnValueAssignable(method),
                        IsStatic = method.IsStatic,
                        ParentFunctionClassName = defaultFormatClassName,
                        ParentFunctionFullyQualifiedClassName = fullyQualifiedClassName,
                        Visibility = method.GetVisibility(),
                        AssemblyIdentity = method.ContainingAssembly.Identity.GetDisplayName(),
                        IsObsolete = isObsolete,
                    };

                    functionList.Add(function);
                }

                return functionList;
            }

            /// <summary>
            /// Returns true if the symbol is Task/Task of T/ValueTask/ValueTask of T.
            /// </summary>
            private bool IsTaskType(ITypeSymbol typeSymbol)
            {
                return
                    SymbolEqualityComparer.Default.Equals(typeSymbol.OriginalDefinition, _knownTypes.TaskType) ||
                    SymbolEqualityComparer.Default.Equals(typeSymbol.OriginalDefinition, _knownTypes.TaskOfTType) ||
                    SymbolEqualityComparer.Default.Equals(typeSymbol.OriginalDefinition, _knownTypes.ValueTaskType) ||
                    SymbolEqualityComparer.Default.Equals(typeSymbol.OriginalDefinition, _knownTypes.ValueTaskOfTTypeOpt);
            }

            /// <summary>
            /// Is the return value of the method assignable to a variable?
            /// Returns True for methods which has Task or void as return type.
            /// </summary>
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
        }
    }
}
