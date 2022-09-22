﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.Azure.Functions.Worker.Sdk.Generators
{
    public partial class FunctionMetadataProviderGenerator
    {
        internal sealed class Emitter
        {

            public string Emit(IReadOnlyList<GeneratorFunctionMetadata> funcMetadata, CancellationToken cancellationToken)
            {
                using var stringWriter = new StringWriter();
                using (var indentedTextWriter = new IndentedTextWriter(stringWriter))
                {
                    var hasHttpTrigger = funcMetadata.Any(funcMetadata => funcMetadata.IsHttpTrigger);

                    SetUpUsings(indentedTextWriter, hasHttpTrigger);

                    // create namespace
                    indentedTextWriter.WriteLine("namespace Microsoft.Azure.Functions.Worker");
                    indentedTextWriter.WriteLine("{");
                    indentedTextWriter.Indent++;

                    // create class that implements IFunctionMetadataProvider
                    indentedTextWriter.WriteLine("public class GeneratedFunctionMetadataProvider : IFunctionMetadataProvider");
                    indentedTextWriter.WriteLine("{");
                    indentedTextWriter.Indent++;

                    WriteGetFunctionsMetadataAsyncMethod(indentedTextWriter, funcMetadata, cancellationToken);

                    indentedTextWriter.Indent--;
                    indentedTextWriter.WriteLine("}");

                    // add method that users can call in startup to register the source-generated file
                    AddRegistrationExtension(indentedTextWriter);

                    indentedTextWriter.Indent--;
                    indentedTextWriter.WriteLine("}");

                    indentedTextWriter.Flush();
                }

                return stringWriter.ToString();
            }

            private void SetUpUsings(IndentedTextWriter indentedTextWriter, bool hasHttpTrigger)
            {
                indentedTextWriter.WriteLine("// <auto-generated/>");
                indentedTextWriter.WriteLine("using System;");
                indentedTextWriter.WriteLine("using System.Collections.Generic;");
                indentedTextWriter.WriteLine("using System.Collections.Immutable;");
                indentedTextWriter.WriteLine("using System.Text.Json;");
                indentedTextWriter.WriteLine("using System.Threading.Tasks;");
                indentedTextWriter.WriteLine("using Microsoft.Azure.Functions.Core;");
                indentedTextWriter.WriteLine("using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;");
                indentedTextWriter.WriteLine("using Microsoft.Extensions.DependencyInjection;");
                indentedTextWriter.WriteLine("using Microsoft.Extensions.Hosting;");

                if (hasHttpTrigger)
                {
                    indentedTextWriter.WriteLine("using Microsoft.Azure.Functions.Worker.Http;");
                }
            }

            private void WriteGetFunctionsMetadataAsyncMethod(IndentedTextWriter indentedTextWriter, IReadOnlyList<GeneratorFunctionMetadata> functionMetadata, CancellationToken cancellationToken)
            {
                indentedTextWriter.WriteLine("public Task<ImmutableArray<IFunctionMetadata>> GetFunctionMetadataAsync(string directory)");
                indentedTextWriter.WriteLine("{");
                indentedTextWriter.Indent++;

                // create list of IFunctionMetadata and populate it
                indentedTextWriter.WriteLine("var metadataList = new List<IFunctionMetadata>();");
                AddFunctionMetadataInfo(indentedTextWriter, functionMetadata, cancellationToken);
                indentedTextWriter.WriteLine("return Task.FromResult(metadataList.ToImmutableArray());");

                indentedTextWriter.Indent--;
                indentedTextWriter.WriteLine("}");
            }

            private void AddFunctionMetadataInfo(IndentedTextWriter indentedTextWriter, IReadOnlyList<GeneratorFunctionMetadata> functionMetadata, CancellationToken cancellationToken)
            {
                var functionCount = 0;

                foreach (var function in functionMetadata)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // we're going to base variable names on Function[Num] because some function names have characters we can't use for a dotnet variable
                    var functionVariableName = "Function" + functionCount.ToString();
                    var functionBindingsListVarName = functionVariableName + "RawBindings";

                    indentedTextWriter.WriteLine($"var {functionBindingsListVarName} = new List<string>();");
                    AddBindingInfo(indentedTextWriter, functionVariableName, functionBindingsListVarName, function.RawBindings);
                    indentedTextWriter.WriteLine($"var {functionVariableName} = new DefaultFunctionMetadata");
                    indentedTextWriter.WriteLine("{");
                    indentedTextWriter.Indent++;
                    indentedTextWriter.WriteLine("FunctionId = Guid.NewGuid().ToString(),");
                    indentedTextWriter.WriteLine("Language = \"dotnet-isolated\",");
                    indentedTextWriter.WriteLine($"Name = \"{function.Name}\",");
                    indentedTextWriter.WriteLine($"EntryPoint = \"{function.EntryPoint}\",");
                    indentedTextWriter.WriteLine($"RawBindings = {functionBindingsListVarName},");
                    indentedTextWriter.WriteLine($"ScriptFile = \"{function.ScriptFile}\"");
                    indentedTextWriter.Indent--;
                    indentedTextWriter.WriteLine("};");
                    indentedTextWriter.WriteLine($"metadataList.Add({functionVariableName});");

                    functionCount++;
                }
            }

            private void AddBindingInfo(IndentedTextWriter indentedTextWriter, string functionVarName, string functionBindingsListVarName, IList<IDictionary<string, string>> bindings)
            {
                var bindingCount = 0;

                foreach (var binding in bindings)
                {
                    var bindingVarName = functionVarName + "binding" + bindingCount.ToString();
                    indentedTextWriter.WriteLine($"var {bindingVarName} = new {{");
                    indentedTextWriter.Indent++;
                    
                    foreach (var key in binding.Keys)
                    {
                        indentedTextWriter.WriteLine($"{key} = {binding[key]},");
                    }

                    indentedTextWriter.Indent--;
                    indentedTextWriter.WriteLine("};");
                    indentedTextWriter.WriteLine($"var {bindingVarName}JSON = JsonSerializer.Serialize({bindingVarName});");
                    indentedTextWriter.WriteLine($"{functionBindingsListVarName}.Add({bindingVarName}JSON);");

                    bindingCount++;
                }
            }

            private void AddRegistrationExtension(IndentedTextWriter indentedTextWriter)
            {
                indentedTextWriter.WriteLine("public static class WorkerHostBuilderFunctionMetadataProviderExtension");
                indentedTextWriter.WriteLine("{");
                indentedTextWriter.Indent++;
                indentedTextWriter.WriteLine(@"\\\<summary>");
                indentedTextWriter.WriteLine(@"\\\ Adds the GeneratedFunctionMetadataProvider to the service collection.");
                indentedTextWriter.WriteLine(@"\\\ During initialization, the worker will return generated funciton metadata instead of relying on the Azure Functions host for function indexing.");
                indentedTextWriter.WriteLine(@"\\\</summary>");
                indentedTextWriter.WriteLine("public static IHostBuilder ConfigureGeneratedFunctionMetadataProvider(this IHostBuilder builder)");
                indentedTextWriter.WriteLine("{");
                indentedTextWriter.Indent++;
                indentedTextWriter.WriteLine("builder.ConfigureServices(s => ");
                indentedTextWriter.WriteLine("{");
                indentedTextWriter.Indent++;
                indentedTextWriter.WriteLine("s.AddSingleton<IFunctionMetadataProvider, GeneratedFunctionMetadataProvider>();");
                indentedTextWriter.Indent--;
                indentedTextWriter.WriteLine("});");
                indentedTextWriter.WriteLine("return builder;");
                indentedTextWriter.Indent--;
                indentedTextWriter.WriteLine("}");
                indentedTextWriter.Indent--;
                indentedTextWriter.WriteLine("}");
            }
        }

    }
}
