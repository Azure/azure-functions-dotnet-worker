﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Sdk.Generators;
using Xunit;

namespace Microsoft.Azure.Functions.SdkGeneratorTests
{
    public partial class FunctionExecutorGeneratorTests
    {
        public class DependentAssemblyTest
        {
            private readonly Assembly[] _referencedAssemblies;

            public DependentAssemblyTest()
            {
                var abstractionsExtension = Assembly.LoadFrom("Microsoft.Azure.Functions.Worker.Extensions.Abstractions.dll");
                var httpExtension = Assembly.LoadFrom("Microsoft.Azure.Functions.Worker.Extensions.Http.dll");
                var hostingExtension = Assembly.LoadFrom("Microsoft.Extensions.Hosting.dll");
                var diExtension = Assembly.LoadFrom("Microsoft.Extensions.DependencyInjection.dll");
                var hostingAbExtension = Assembly.LoadFrom("Microsoft.Extensions.Hosting.Abstractions.dll");
                var diAbExtension = Assembly.LoadFrom("Microsoft.Extensions.DependencyInjection.Abstractions.dll");
                var dependentAssembly = Assembly.LoadFrom("DependentAssemblyWithFunctions.dll");

                _referencedAssemblies = new[]
                {
                    abstractionsExtension,
                    httpExtension,
                    hostingExtension,
                    hostingAbExtension,
                    diExtension,
                    diAbExtension,
                    dependentAssembly
                };
            }

            [Fact]
            public async Task FunctionsFromDependentAssembly()
            {
                const string inputSourceCode = """
                                               using System;
                                               using Microsoft.Azure.Functions.Worker;
                                               using Microsoft.Azure.Functions.Worker.Http;
                                               namespace MyCompany
                                               {
                                                   public class MyHttpTriggers
                                                   {
                                                       [Function("FunctionA")]
                                                       public HttpResponseData Foo([HttpTrigger(AuthorizationLevel.User, "get")] HttpRequestData r, FunctionContext c)
                                                       {
                                                           return r.CreateResponse(System.Net.HttpStatusCode.OK);
                                                       }
                                                   }
                                               }
                                               """;
                var expected = $$"""
                                       // <auto-generated/>
                                       using System;
                                       using System.Threading.Tasks;
                                       using System.Collections.Generic;
                                       using Microsoft.Extensions.Hosting;
                                       using Microsoft.Extensions.DependencyInjection;
                                       using Microsoft.Azure.Functions.Worker;
                                       using Microsoft.Azure.Functions.Worker.Context.Features;
                                       using Microsoft.Azure.Functions.Worker.Invocation;
                                       namespace TestProject
                                       {
                                           [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
                                           [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
                                           internal class DirectFunctionExecutor : IFunctionExecutor
                                           {
                                               private readonly IFunctionActivator _functionActivator;
                                               private Lazy<IFunctionExecutor> _defaultExecutor;
                                               private readonly Dictionary<string, Type> types = new()
                                               {
                                                   { "MyCompany.MyHttpTriggers", Type.GetType("MyCompany.MyHttpTriggers, TestProject, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")! },
                                                   { "DependentAssemblyWithFunctions.DependencyFunction", Type.GetType("DependentAssemblyWithFunctions.DependencyFunction, DependentAssemblyWithFunctions, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null")! },
                                                   { "MyCompany.MyProduct.MyApp.HttpFunctions", Type.GetType("MyCompany.MyProduct.MyApp.HttpFunctions, DependentAssemblyWithFunctions, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null")! },
                                                   { "MyCompany.MyProduct.MyApp.Foo.Bar", Type.GetType("MyCompany.MyProduct.MyApp.Foo.Bar, DependentAssemblyWithFunctions, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null")! }
                                               };
                                       
                                               public DirectFunctionExecutor(IFunctionActivator functionActivator)
                                               {
                                                   _functionActivator = functionActivator ?? throw new ArgumentNullException(nameof(functionActivator));
                                               }
                                       
                                               /// <inheritdoc/>
                                               public async ValueTask ExecuteAsync(FunctionContext context)
                                               {
                                                   var inputBindingFeature = context.Features.Get<IFunctionInputBindingFeature>()!;
                                                   var inputBindingResult = await inputBindingFeature.BindFunctionInputAsync(context)!;
                                                   var inputArguments = inputBindingResult.Values;
                                                   _defaultExecutor = new Lazy<IFunctionExecutor>(() => CreateDefaultExecutorInstance(context));
                                       
                                                   if (string.Equals(context.FunctionDefinition.EntryPoint, "MyCompany.MyHttpTriggers.Foo", StringComparison.Ordinal))
                                                   {
                                                       var instanceType = types["MyCompany.MyHttpTriggers"];
                                                       var i = _functionActivator.CreateInstance(instanceType, context) as global::MyCompany.MyHttpTriggers;
                                                       context.GetInvocationResult().Value = i.Foo((global::Microsoft.Azure.Functions.Worker.Http.HttpRequestData)inputArguments[0], (global::Microsoft.Azure.Functions.Worker.FunctionContext)inputArguments[1]);
                                                   }
                                                   else if (string.Equals(context.FunctionDefinition.EntryPoint, "DependentAssemblyWithFunctions.DependencyFunction.Run", StringComparison.Ordinal))
                                                   {
                                                       var instanceType = types["DependentAssemblyWithFunctions.DependencyFunction"];
                                                       var i = _functionActivator.CreateInstance(instanceType, context) as global::DependentAssemblyWithFunctions.DependencyFunction;
                                                       context.GetInvocationResult().Value = i.Run((global::Microsoft.Azure.Functions.Worker.Http.HttpRequestData)inputArguments[0]);
                                                   }
                                                   else if (string.Equals(context.FunctionDefinition.EntryPoint, "DependentAssemblyWithFunctions.InternalFunction.Run", StringComparison.Ordinal))
                                                   {
                                                       await _defaultExecutor.Value.ExecuteAsync(context);
                                                   }
                                                   else if (string.Equals(context.FunctionDefinition.EntryPoint, "DependentAssemblyWithFunctions.StaticFunction.Run", StringComparison.Ordinal))
                                                   {
                                                       context.GetInvocationResult().Value = global::DependentAssemblyWithFunctions.StaticFunction.Run((global::Microsoft.Azure.Functions.Worker.Http.HttpRequestData)inputArguments[0], (global::Microsoft.Azure.Functions.Worker.FunctionContext)inputArguments[1]);
                                                   }
                                                   else if (string.Equals(context.FunctionDefinition.EntryPoint, "MyCompany.MyProduct.MyApp.HttpFunctions.Run", StringComparison.Ordinal))
                                                   {
                                                       var instanceType = types["MyCompany.MyProduct.MyApp.HttpFunctions"];
                                                       var i = _functionActivator.CreateInstance(instanceType, context) as global::MyCompany.MyProduct.MyApp.HttpFunctions;
                                                       context.GetInvocationResult().Value = i.Run((global::Microsoft.Azure.Functions.Worker.Http.HttpRequestData)inputArguments[0]);
                                                   }
                                                   else if (string.Equals(context.FunctionDefinition.EntryPoint, "MyCompany.MyProduct.MyApp.Foo.Bar.Run", StringComparison.Ordinal))
                                                   {
                                                       var instanceType = types["MyCompany.MyProduct.MyApp.Foo.Bar"];
                                                       var i = _functionActivator.CreateInstance(instanceType, context) as global::MyCompany.MyProduct.MyApp.Foo.Bar;
                                                       context.GetInvocationResult().Value = i.Run((global::Microsoft.Azure.Functions.Worker.Http.HttpRequestData)inputArguments[0]);
                                                   }
                                               }
                                       
                                               private IFunctionExecutor CreateDefaultExecutorInstance(FunctionContext context)
                                               {
                                                   var defaultExecutorFullName = "Microsoft.Azure.Functions.Worker.Invocation.DefaultFunctionExecutor, Microsoft.Azure.Functions.Worker.Core, Version=1.16.0.0, Culture=neutral, PublicKeyToken=551316b6919f366c";
                                                   var defaultExecutorType = Type.GetType(defaultExecutorFullName);
                                       
                                                   return ActivatorUtilities.CreateInstance(context.InstanceServices, defaultExecutorType) as IFunctionExecutor;
                                               }
                                           }
                                       {{GetExpectedExtensionMethodCode()}}
                                       }
                                       """.Replace("'", "\"");

                await TestHelpers.RunTestAsync<FunctionExecutorGenerator>(
                    _referencedAssemblies,
                    inputSourceCode,
                    Constants.FileNames.GeneratedFunctionExecutor,
                    expected);
            }
        }
    }
}
