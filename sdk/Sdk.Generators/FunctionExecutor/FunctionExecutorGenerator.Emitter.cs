﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
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
            internal static string Emit(GeneratorExecutionContext context, IEnumerable<ExecutableFunction> functions, bool includeAutoRegistrationCode)
            {

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
                             internal class DirectFunctionExecutor : IFunctionExecutor
                             {
                                 private readonly IFunctionActivator _functionActivator;
                                 {{GetTypesDictionary(functions)}}
                                 public DirectFunctionExecutor(IFunctionActivator functionActivator)
                                 {
                                     _functionActivator = functionActivator ?? throw new ArgumentNullException(nameof(functionActivator));
                                 }

                                 /// <inheritdoc/>
                                 public async ValueTask ExecuteAsync(FunctionContext context)
                                 {
                                     {{GetMethodBody(functions)}}
                                 }
                             }

                             /// <summary>
                             /// Extension methods to enable registration of the custom <see cref="IFunctionExecutor"/> implementation generated for the current worker.
                             /// </summary>
                             public static class FunctionExecutorHostBuilderExtensions
                             {
                                 ///<summary>
                                 /// Configures an optimized function executor to the invocation pipeline.
                                 ///</summary>
                                 public static IHostBuilder ConfigureGeneratedFunctionExecutor(this IHostBuilder builder)
                                 {
                                     return builder.ConfigureServices(s => 
                                     {
                                         s.AddSingleton<IFunctionExecutor, DirectFunctionExecutor>();
                                     });
                                 }
                             }{{GetAutoConfigureStartupClass(includeAutoRegistrationCode)}}
                         }
                         """;

                return result;
            }

            private static string GetTypesDictionary(IEnumerable<ExecutableFunction> functions)
            {
                var classNames = functions.Where(f => !f.IsStatic).Select(f => f.ParentFunctionClassName).Distinct();
                if (!classNames.Any())
                {
                    return """

                     """;
                }

                return $$"""
                private readonly Dictionary<string, Type> types = new()
                        {
                           {{string.Join($",{Environment.NewLine}           ", classNames.Select(c => $$""" { "{{c}}", Type.GetType("{{c}}")! }"""))}}
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
                                      public class FunctionExecutorAutoStartup : IAutoConfigureStartup
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

            private static string GetMethodBody(IEnumerable<ExecutableFunction> functions)
            {
                var sb = new StringBuilder();
                sb.Append(
                   """
                var inputBindingFeature = context.Features.Get<IFunctionInputBindingFeature>()!;
                            var inputBindingResult = await inputBindingFeature.BindFunctionInputAsync(context)!;
                            var inputArguments = inputBindingResult.Values;

                """);
                foreach (ExecutableFunction function in functions)
                {
                    sb.Append($$"""

                        if (string.Equals(context.FunctionDefinition.EntryPoint, "{{function.EntryPoint}}", StringComparison.OrdinalIgnoreCase))
                        {
            """);

                    int functionParamCounter = 0;
                    var functionParamList = new List<string>();
                    foreach (var argumentTypeName in function.ParameterTypeNames)
                    {
                        functionParamList.Add($"({argumentTypeName})inputArguments[{functionParamCounter++}]");
                    }
                    var methodParamsStr = string.Join(", ", functionParamList);

                    if (!function.IsStatic)
                    {
                        sb.Append($$"""

                                var instanceType = types["{{function.ParentFunctionClassName}}"];
                                var i = _functionActivator.CreateInstance(instanceType, context) as {{function.ParentFunctionFullyQualifiedClassName}};
                """);
                    }

                    sb.Append(@"
                ");

                    if (function.IsReturnValueAssignable)
                    {
                        sb.Append(@$"context.GetInvocationResult().Value = ");
                    }
                    if (function.ShouldAwait)
                    {
                        sb.Append("await ");
                    }

                    sb.Append(function.IsStatic
                        ? @$"{function.ParentFunctionFullyQualifiedClassName}.{function.MethodName}({methodParamsStr});
            }}"
                        : $@"i.{function.MethodName}({methodParamsStr});
            }}");
                }

                return sb.ToString();
            }
        }
    }
}
