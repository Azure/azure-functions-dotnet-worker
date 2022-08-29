// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Microsoft.Azure.Functions.Worker.Sdk.Generators
{
    public partial class ExtensionStartupRunnerGenerator
    {
        internal static class Parser
        {
            /// <summary>
            /// Fully qualified name of the above "WorkerExtensionStartupAttribute" attribute.
            /// </summary>
            private const string AttributeTypeFullName =
                "Microsoft.Azure.Functions.Worker.Core.WorkerExtensionStartupAttribute";

            /// <summary>
            /// Fully qualified name of the base type which extension startup classes should implement.
            /// </summary>
            private const string StartupBaseClassName = "Microsoft.Azure.Functions.Worker.Core.WorkerExtensionStartup";

            internal static bool IsAssemblyWithExtensionStartupAttribute(IAssemblySymbol assemblySymbol,
                Compilation compilation, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var extensionStartupAttribute = GetExtensionStartupAttributeData(assemblySymbol, compilation);

                return extensionStartupAttribute != null;
            }

            private static AttributeData? GetExtensionStartupAttributeData(IAssemblySymbol assemblySymbol,
                Compilation compilation)
            {
                INamedTypeSymbol? extensionStartupAttributeSymbol = compilation.GetTypeByMetadataName(AttributeTypeFullName);

                if (extensionStartupAttributeSymbol is null)
                {
                    return null;
                }

                var extensionStartupAttribute = assemblySymbol.GetAttributes()
                    .FirstOrDefault(attr =>
                        extensionStartupAttributeSymbol.Equals(attr.AttributeClass, SymbolEqualityComparer.Default));

                return extensionStartupAttribute;
            }

            /// <summary>
            /// Build an instance of StartupTypeInfo from the assembly symbol.
            /// </summary>
            internal static StartupTypeInfo CreateStartupTypeInfoFromAssemblySymbol(Compilation compilation,
                IAssemblySymbol assemblySymbol, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var extensionStartupAttribute = GetExtensionStartupAttributeData(assemblySymbol, compilation);
                if (extensionStartupAttribute is null)
                {
                    return default;
                }

                // WorkerExtensionStartupAttribute has a constructor with one param, the type of startup implementation class.
                var firstConstructorParam = extensionStartupAttribute.ConstructorArguments[0];
                if (firstConstructorParam.Value is not INamedTypeSymbol startupTypeNamedTypeSymbol)
                {
                    return default;
                }

                return CreateStartupTypeInfoFromType(startupTypeNamedTypeSymbol, compilation);
            }

            private static StartupTypeInfo CreateStartupTypeInfoFromType(INamedTypeSymbol namedTypeSymbol,
                Compilation compilation)
            {

                var builder = ImmutableArray.CreateBuilder<Diagnostic>();

                // Check for any diagnostics we need to report about this type.                

                // 1) Check public parameterless constructor exist for the type.
                var constructorExist = namedTypeSymbol.InstanceConstructors
                    .Any(c => c.Parameters.Length == 0 &&
                              c.DeclaredAccessibility == Accessibility.Public);
                if (!constructorExist)
                {
                    var diagnostic = Diagnostic.Create(DiagnosticDescriptors.ConstructorMissing, Location.None,
                        namedTypeSymbol.ToDisplayString());
                    builder.Add(diagnostic);
                }

                // 2) Check the extension startup class implements WorkerExtensionStartup abstract class.
                INamedTypeSymbol? startupBaseTypeSymbol = compilation.GetTypeByMetadataName(StartupBaseClassName);
                
                if (namedTypeSymbol.BaseType == null ||
                    !namedTypeSymbol.BaseType.Equals(startupBaseTypeSymbol, SymbolEqualityComparer.Default))
                {
                    var diagnostic = Diagnostic.Create(DiagnosticDescriptors.IncorrectBaseType, Location.None,
                        namedTypeSymbol.ToDisplayString(), StartupBaseClassName);
                    builder.Add(diagnostic);
                }
                
                return new StartupTypeInfo(namedTypeSymbol.ToDisplayString(), builder.ToImmutable());
            }
        }

        /// <summary>
        /// A type to represent the data about the extension startup types we want to generate code for.
        /// </summary>
        internal struct StartupTypeInfo
        {
            internal StartupTypeInfo(string startupTypeName, ImmutableArray<Diagnostic> diagnostics) 
            {
                StartupTypeName = startupTypeName;
                Diagnostics = diagnostics;
            }

            /// <summary>
            /// Gets the full name of the startup type.
            /// ex: Microsoft.Azure.Functions.Worker.Extensions.DurableTask.DurableTaskExtensionStartup
            /// </summary>
            internal string StartupTypeName { get; }

            /// <summary>
            /// Gets a collection of diagnostic entries associated with this startup type.
            /// </summary>
            internal ImmutableArray<Diagnostic> Diagnostics { get; }
        }
    }
}
