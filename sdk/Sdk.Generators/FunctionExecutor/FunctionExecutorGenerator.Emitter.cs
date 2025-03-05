﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Microsoft.Azure.Functions.Worker.Sdk.Generators
{
    public partial class FunctionExecutorGenerator
    {
        internal static class Emitter
        {
            private const string WorkerCoreAssemblyName = "Microsoft.Azure.Functions.Worker.Core";

            internal static string Emit(GeneratorExecutionContext context, IEnumerable<ExecutableFunction> executableFunctions, bool includeAutoRegistrationCode)
            {
                var functions = executableFunctions.ToList();
                var defaultExecutorNeeded = functions.Any(f => f.Visibility == FunctionMethodVisibility.PublicButContainingTypeNotVisible);

                string result = $$"""
                         // <auto-generated/>
                         using System;
                         using System.Threading.Tasks;
                         using System.Collections.Generic;
                         using Microsoft.Extensions.Hosting;
                         using Microsoft.Extensions.DependencyInjection;
                         using Microsoft.Azure.Functions.Worker;
                         using Microsoft.Azure.Functions.Worker.Context.Features;
                         using Microsoft.Azure.Functions.Worker.Invocation;
                         namespace {{FunctionsUtil.GetNamespaceForGeneratedCode(context)}}
                         {
                             [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
                             {{Constants.GeneratedCodeAttribute}}
                             internal class DirectFunctionExecutor : global::Microsoft.Azure.Functions.Worker.Invocation.IFunctionExecutor
                             {
                                 private readonly global::Microsoft.Azure.Functions.Worker.IFunctionActivator _functionActivator;{{(defaultExecutorNeeded ? $"{Constants.NewLine}        private Lazy<global::Microsoft.Azure.Functions.Worker.Invocation.IFunctionExecutor> _defaultExecutor;" : string.Empty)}}
                                 {{GetTypesDictionary(functions)}}
                                 public DirectFunctionExecutor(global::Microsoft.Azure.Functions.Worker.IFunctionActivator functionActivator)
                                 {
                                     _functionActivator = functionActivator ?? throw new global::System.ArgumentNullException(nameof(functionActivator));
                                 }

                                 /// <inheritdoc/>
                                 public async global::System.Threading.Tasks.ValueTask ExecuteAsync(global::Microsoft.Azure.Functions.Worker.FunctionContext context)
                                 {
                                     {{GetMethodBody(functions, defaultExecutorNeeded)}}
                                 }{{(defaultExecutorNeeded ? $"{Constants.NewLine}{EmitCreateDefaultExecutorMethod(context)}" : string.Empty)}}
                             }

                             /// <summary>
                             /// Extension methods to enable registration of the custom <see cref="IFunctionExecutor"/> implementation generated for the current worker.
                             /// </summary>
                             {{Constants.GeneratedCodeAttribute}}
                             public static class FunctionExecutorHostBuilderExtensions
                             {
                                 ///<summary>
                                 /// Configures an optimized function executor to the invocation pipeline.
                                 ///</summary>
                                 public static IHostBuilder ConfigureGeneratedFunctionExecutor(this IHostBuilder builder)
                                 {
                                     return builder.ConfigureServices(s => 
                                     {
                                         s.AddSingleton<global::Microsoft.Azure.Functions.Worker.Invocation.IFunctionExecutor, DirectFunctionExecutor>();
                                     });
                                 }
                             }{{GetAutoConfigureStartupClass(includeAutoRegistrationCode)}}
                         }
                         """;

                return result;
            }

            private static string EmitCreateDefaultExecutorMethod(GeneratorExecutionContext context)
            {
                var workerCoreAssembly = context.Compilation.SourceModule.ReferencedAssemblySymbols.Single(a => a.Name == WorkerCoreAssemblyName);
                var assemblyIdentity = workerCoreAssembly.Identity;

                return $$"""

                            private global::Microsoft.Azure.Functions.Worker.Invocation.IFunctionExecutor CreateDefaultExecutorInstance(global::Microsoft.Azure.Functions.Worker.FunctionContext context)
                            {
                                var defaultExecutorFullName = "Microsoft.Azure.Functions.Worker.Invocation.DefaultFunctionExecutor, {{assemblyIdentity}}";
                                var defaultExecutorType = global::System.Type.GetType(defaultExecutorFullName);

                                return ActivatorUtilities.CreateInstance(context.InstanceServices, defaultExecutorType) as global::Microsoft.Azure.Functions.Worker.Invocation.IFunctionExecutor;
                            }
                    """;
            }

            private static string GetTypesDictionary(IEnumerable<ExecutableFunction> functions)
            {
                // Build a dictionary of type names and its full qualified names (including assembly identity)
                var typesDict = functions
                                    .Where(f => !f.IsStatic)
                                    .GroupBy(f => f.ParentFunctionClassName)
                                    .ToDictionary(k => k.First().ParentFunctionClassName, v => v.First().AssemblyIdentity);

                if (typesDict.Count == 0)
                {
                    return "";
                }

                return $$"""
                private readonly Dictionary<string, Type> types = new Dictionary<string, Type>()
                        {
                           {{string.Join($",{Constants.NewLine}           ", typesDict.Select(c => $$""" { "{{c.Key}}", Type.GetType("{{c.Key}}, {{c.Value}}") }"""))}}
                        };

                """;
            }

            private static string GetAutoConfigureStartupClass(bool includeAutoRegistrationCode)
            {
                if (includeAutoRegistrationCode)
                {
                    string result = $$"""

                                      /// <summary>
                                      /// Auto startup class to register the custom <see cref="IFunctionExecutor"/> implementation generated for the current worker.
                                      /// </summary>
                                      [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
                                      {{Constants.GeneratedCodeAttribute}}
                                      public class FunctionExecutorAutoStartup : global::Microsoft.Azure.Functions.Worker.IAutoConfigureStartup
                                      {
                                          /// <summary>
                                          /// Configures the <see cref="IHostBuilder"/> to use the custom <see cref="IFunctionExecutor"/> implementation generated for the current worker.
                                          /// </summary>
                                          /// <param name="hostBuilder">The <see cref="IHostBuilder"/> instance to use for service registration.</param>
                                          public void Configure(IHostBuilder hostBuilder)
                                          {
                                              hostBuilder.ConfigureGeneratedFunctionExecutor();
                                          }
                                      }
                                  """;

                    return result;
                }
                return "";
            }

            private static string GetMethodBody(IEnumerable<ExecutableFunction> functions, bool anyDefaultExecutor)
            {
                var sb = new StringBuilder();
                sb.Append(
                   $"""
                var inputBindingFeature = context.Features.Get<global::Microsoft.Azure.Functions.Worker.Context.Features.IFunctionInputBindingFeature>();
                            var inputBindingResult = await inputBindingFeature.BindFunctionInputAsync(context);
                            var inputArguments = inputBindingResult.Values;
                {(anyDefaultExecutor ? $"            _defaultExecutor = new Lazy<global::Microsoft.Azure.Functions.Worker.Invocation.IFunctionExecutor>(() => CreateDefaultExecutorInstance(context));{Constants.NewLine}" : string.Empty)}
                """);

                foreach (ExecutableFunction function in functions)
                {
                    var fast = function.Visibility == FunctionMethodVisibility.Public;
                    sb.Append($$"""

                        if (string.Equals(context.FunctionDefinition.EntryPoint, "{{function.EntryPoint}}", StringComparison.Ordinal))
                        {
            {{(fast ? EmitFastPath(function) : EmitSlowPath())}}
                            return;
                        }
            """);
                }

                return sb.ToString();
            }

            private static string EmitFastPath(ExecutableFunction function)
            {
                var sb = new StringBuilder();

                if (!function.IsStatic)
                {
                    sb.Append($"""
                                                var instanceType = types["{function.ParentFunctionClassName}"];
                                                var i = _functionActivator.CreateInstance(instanceType, context) as {function.ParentFunctionFullyQualifiedClassName};

                                """);
                }

                if (function.IsObsolete)
                {
                    sb.AppendLine("#pragma warning disable CS0618");
                }

                sb.Append("                ");

                if (function.IsReturnValueAssignable)
                {
                    sb.Append("context.GetInvocationResult().Value = ");
                }

                if (function.ShouldAwait)
                {
                    sb.Append("await ");
                }

                // Parameters
                int functionParamCounter = 0;
                var functionParamList = new List<string>();

                foreach (var argumentTypeName in function.ParameterTypeNames)
                {
                    functionParamList.Add($"({argumentTypeName})inputArguments[{functionParamCounter++}]");
                }

                var methodParamsStr = string.Join(", ", functionParamList);

                sb.Append(function.IsStatic
                    ? $"{function.ParentFunctionFullyQualifiedClassName}.{function.MethodName}({methodParamsStr});"
                    : $"i.{function.MethodName}({methodParamsStr});");
                
                if (function.IsObsolete)
                {
                    // Environment.NewLine - Environment is banned for use in Analyzers
                    sb.AppendLine();
                    sb.Append("#pragma warning restore CS0618");
                }

                return sb.ToString();
            }

            private static string EmitSlowPath()
            {
                return "                await _defaultExecutor.Value.ExecuteAsync(context);";
            }
        }
    }
}
