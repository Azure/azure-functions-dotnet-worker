// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.Azure.Functions.Worker.Sdk.Generators
{
    /// <summary>
    /// Visits all symbols from referenced assemblies and returns all methods which are valid Azure Functions.
    /// </summary>
    internal sealed class ReferencedAssemblyMethodVisitor : SymbolVisitor
    {
        private readonly Compilation _compilation;

        /// <summary>
        /// Gets all methods which are valid Azure Functions.
        /// </summary>
        internal readonly List<IMethodSymbol> FunctionMethods = new();

        internal ReferencedAssemblyMethodVisitor(Compilation compilation)
        {
            _compilation = compilation ?? throw new ArgumentNullException(nameof(compilation));
        }

        public override void VisitModule(IModuleSymbol moduleSymbol)
        {
            foreach (var assemblySymbol in moduleSymbol.ReferencedAssemblySymbols)
            {
                assemblySymbol.Accept(this);
            }
        }

        public override void VisitAssembly(IAssemblySymbol symbol)
        {
            var namespaceSymbol = symbol.GlobalNamespace;
            namespaceSymbol.Accept(this);
        }

        public override void VisitNamespace(INamespaceSymbol symbol)
        {
            // Get classes in this namespace or child namespaces
            var classesOrNamespaces = symbol.GetMembers()
                .Where(a => a.Kind is SymbolKind.Namespace or SymbolKind.NamedType);

            foreach (var nsChild in classesOrNamespaces)
            {
                nsChild.Accept(this);
            }
        }

        public override void VisitNamedType(INamedTypeSymbol symbol)
        {
            // Get methods in this class or nested child classes
            var methodsOrClasses = symbol.GetMembers()
                .Where(a => a.Kind is SymbolKind.NamedType or SymbolKind.Method);

            foreach (var childSymbol in methodsOrClasses)
            {
                childSymbol.Accept(this);
            }
        }

        public override void VisitMethod(IMethodSymbol methodSymbol)
        {
            if (methodSymbol.MethodKind == MethodKind.Ordinary &&
                FunctionsUtil.IsFunctionSymbol(methodSymbol, _compilation))
            {
                FunctionMethods.Add(methodSymbol);
            }
        }
    }
}
