﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
namespace Microsoft.Azure.Functions.Worker.Sdk.Generators
{
    /// <summary>
    /// Generates a class with a method which has code to call the "Configure" method
    /// of each of the participating extension's "WorkerExtensionStartup" implementations.
    /// Also adds the assembly attribute "WorkerExtensionStartupCodeExecutorInfo"
    /// and pass the information(the type) about the class we generated.
    /// We are also inheriting the generated class from the WorkerExtensionStartup class.
    /// (This is the same abstract class extension authors will implement for their extension specific startup code)
    /// We need the same signature as the extension's implementation as our class is an uber class which internally
    /// calls each of the extension's implementations.
    /// </summary>

    // Sample code generated (with one extensions participating in startup hook)
    // There will be one try-catch block for each extension participating in startup hook.

    //[assembly: WorkerExtensionStartupCodeExecutorInfo(typeof(Microsoft.Azure.Functions.Worker.WorkerExtensionStartupCodeExecutor))]
    //
    //internal class WorkerExtensionStartupCodeExecutor : WorkerExtensionStartup
    //{
    //    public override void Configure(IFunctionsWorkerApplicationBuilder applicationBuilder)
    //    {
    //        try
    //        {
    //            new Microsoft.Azure.Functions.Worker.Extensions.Http.MyHttpExtensionStartup().Configure(applicationBuilder);
    //        }
    //        catch (Exception ex)
    //        {
    //            Console.Error.WriteLine("Error calling Configure on Microsoft.Azure.Functions.Worker.Extensions.Http.MyHttpExtensionStartup instance." + ex.ToString());
    //        }
    //    }
    //}

    [Generator]
    public class ExtensionStartupRunnerGenerator : ISourceGenerator
    {
        /// <summary>
        /// The attribute which extension authors will apply on an assembly which contains their startup type.
        /// </summary>
        private string attributeTypeName = "WorkerExtensionStartupAttribute";

        /// <summary>
        /// Fully qualified name of the above "WorkerExtensionStartupAttribute" attribute.
        /// </summary>
        private string attributeTypeFullName = "Microsoft.Azure.Functions.Worker.Core.WorkerExtensionStartupAttribute";

        /// <summary>
        /// Fully qualified name of the base type which extension startup classes should implement.
        /// </summary>
        private string startupBaseClassName = "Microsoft.Azure.Functions.Worker.Core.WorkerExtensionStartup";

        public void Execute(GeneratorExecutionContext context)
        {
            var startupTypes = GetExtensionStartupTypes(context);

            if (!startupTypes.Any())
            {
                return;
            }

            SourceText sourceText;
            using (var stringWriter = new StringWriter())
            using (var indentedTextWriter = new IndentedTextWriter(stringWriter))
            {
                indentedTextWriter.WriteLine("// <auto-generated/>");
                indentedTextWriter.WriteLine("using System;");
                indentedTextWriter.WriteLine("using Microsoft.Azure.Functions.Worker.Core;");
                WriteAssemblyAttribute(indentedTextWriter);
                indentedTextWriter.WriteLine("namespace Microsoft.Azure.Functions.Worker");
                indentedTextWriter.WriteLine("{");
                indentedTextWriter.Indent++;
                WriteStartupCodeExecutorClass(indentedTextWriter, startupTypes);
                indentedTextWriter.Indent--;
                indentedTextWriter.WriteLine("}");

                indentedTextWriter.Flush();
                sourceText = SourceText.From(stringWriter.ToString(), encoding: Encoding.UTF8);
            }

            // Add the source code to the compilation
            context.AddSource($"WorkerExtensionStartupCodeExecutor.g.cs", sourceText);
        }

        /// <summary>
        /// Gets the extension startup implementation type info from each of the participating extensions.
        /// Each entry in the return type collection includes full type name 
        /// & a potential error message if the startup type is not valid.
        /// </summary>
        private IEnumerable<(string TypeName, string? Error)> GetExtensionStartupTypes(GeneratorExecutionContext context)
        {
            List<(string TypeName, string? Error)>? typeInfoList = null;

            // Extension authors should decorate their assembly with "WorkerExtensionStartup" attribute
            // if they want to participate in startup.
            foreach (var assembly in context.Compilation.SourceModule.ReferencedAssemblySymbols)
            {
                var extensionStartupAttribute = assembly.GetAttributes()
                                                        .FirstOrDefault(a => a.AttributeClass?.Name == attributeTypeName &&
                                                                        //Call GetFullName only if class name matches.
                                                                        a.AttributeClass.GetFullName() == attributeTypeFullName);
                if (extensionStartupAttribute != null)
                {
                    // WorkerExtensionStartupAttribute has a constructor with one param, the type of startup implementation class.
                    var firstConstructorParam = extensionStartupAttribute.ConstructorArguments[0];
                    if (firstConstructorParam.Value is not ITypeSymbol typeSymbol)
                    {
                        continue;
                    }

                    var fullTypeName = typeSymbol.ToDisplayString();
                    var errorMessage = GetErrorIfTypeIsNotValid(typeSymbol);

                    typeInfoList ??= new List<(string TypeName, string? Error)>();
                    typeInfoList.Add((TypeName: fullTypeName, Error: errorMessage));
                }
            }

            return typeInfoList ?? Enumerable.Empty<(string TypeName, string? Error)>();
        }

        /// <summary>
        /// Check the type to see it is valid and return an error message of the first failure.
        /// </summary>
        private string? GetErrorIfTypeIsNotValid(ITypeSymbol typeSymbol)
        {
            if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
            {
                // Check public parameterless constructor exist for the type.
                var constructorExist = namedTypeSymbol.InstanceConstructors
                                                  .Any(c => c.Parameters.Length == 0 &&
                                                            c.DeclaredAccessibility == Accessibility.Public);
                if (!constructorExist)
                {
                    return $"{typeSymbol.ToDisplayString()} class must have a public parameterless constructor.";
                }

                // Check the extension startup class implements WorkerExtensionStartup abstract class.
                if (!(namedTypeSymbol.BaseType!.GetFullName().Equals(startupBaseClassName)))
                {
                    return $"{typeSymbol.ToDisplayString()} must be a type which implements {startupBaseClassName}.";
                }
            }

            return null;
        }

        /// <summary>
        /// Writes an assembly attribute with type information about our auto generated WorkerExtensionStartupCodeExecutor class.
        /// </summary>
        private static void WriteAssemblyAttribute(IndentedTextWriter textWriter)
        {
            textWriter.WriteLine(
                "[assembly: WorkerExtensionStartupCodeExecutorInfo(typeof(Microsoft.Azure.Functions.Worker.WorkerExtensionStartupCodeExecutor))]");
        }

        /// <summary>
        /// Writes a class with code which calls the Configure method on each implementation of participating extensions.
        /// We also have it implement the same "IWorkerExtensionStartup" interface which extension authors implement.
        /// </summary>
        private static void WriteStartupCodeExecutorClass(IndentedTextWriter textWriter, IEnumerable<(string TypeName, string? Error)> types)
        {
            textWriter.WriteLine("internal class WorkerExtensionStartupCodeExecutor : WorkerExtensionStartup");
            textWriter.WriteLine("{");
            textWriter.Indent++;
            textWriter.WriteLine("public override void Configure(IFunctionsWorkerApplicationBuilder applicationBuilder)");
            textWriter.WriteLine("{");
            textWriter.Indent++;

            foreach (var type in types)
            {
                textWriter.WriteLine("try");
                textWriter.WriteLine("{");
                textWriter.Indent++;
                if (type.Error != null)
                {
                    textWriter.WriteLine($"throw new InvalidOperationException(\"{type.Error}\");");
                }
                else
                {
                    textWriter.WriteLine($"new {type.TypeName}().Configure(applicationBuilder);");
                }
                textWriter.Indent--;
                textWriter.WriteLine("}");
                textWriter.WriteLine("catch (Exception ex)");
                textWriter.WriteLine("{");
                textWriter.Indent++;
                textWriter.WriteLine($"Console.Error.WriteLine(\"Error calling Configure on {type.TypeName} instance.\"+ex.ToString());");
                textWriter.Indent--;
                textWriter.WriteLine("}");
            }

            textWriter.Indent--;
            textWriter.WriteLine("}");
            textWriter.Indent--;
            textWriter.WriteLine("}");
        }

        public void Initialize(GeneratorInitializationContext context)
        {
        }
    }
}
