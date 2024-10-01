﻿//HintName: GeneratedFunctionExecutor.g.cs
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
    internal class DirectFunctionExecutor : global::Microsoft.Azure.Functions.Worker.Invocation.IFunctionExecutor
    {
        private readonly global::Microsoft.Azure.Functions.Worker.IFunctionActivator _functionActivator;
        
        public DirectFunctionExecutor(global::Microsoft.Azure.Functions.Worker.IFunctionActivator functionActivator)
        {
            _functionActivator = functionActivator ?? throw new global::System.ArgumentNullException(nameof(functionActivator));
        }

        /// <inheritdoc/>
        public async global::System.Threading.Tasks.ValueTask ExecuteAsync(global::Microsoft.Azure.Functions.Worker.FunctionContext context)
        {
            var inputBindingFeature = context.Features.Get<global::Microsoft.Azure.Functions.Worker.Context.Features.IFunctionInputBindingFeature>();
            var inputBindingResult = await inputBindingFeature.BindFunctionInputAsync(context);
            var inputArguments = inputBindingResult.Values;

            if (string.Equals(context.FunctionDefinition.EntryPoint, "FunctionApp26.MyQTriggers.MyTaskStaticMethod", StringComparison.Ordinal))
            {
                await global::FunctionApp26.MyQTriggers.MyTaskStaticMethod((string)inputArguments[0]);
            }
            else if (string.Equals(context.FunctionDefinition.EntryPoint, "FunctionApp26.MyQTriggers.MyAsyncStaticMethod", StringComparison.Ordinal))
            {
                context.GetInvocationResult().Value = await global::FunctionApp26.MyQTriggers.MyAsyncStaticMethod((string)inputArguments[0]);
            }
            else if (string.Equals(context.FunctionDefinition.EntryPoint, "FunctionApp26.MyQTriggers.MyVoidStaticMethod", StringComparison.Ordinal))
            {
                global::FunctionApp26.MyQTriggers.MyVoidStaticMethod((string)inputArguments[0]);
            }
            else if (string.Equals(context.FunctionDefinition.EntryPoint, "FunctionApp26.MyQTriggers.MyAsyncStaticMethodWithReturn", StringComparison.Ordinal))
            {
                context.GetInvocationResult().Value = await global::FunctionApp26.MyQTriggers.MyAsyncStaticMethodWithReturn((string)inputArguments[0], (string)inputArguments[1]);
            }
            else if (string.Equals(context.FunctionDefinition.EntryPoint, "FunctionApp26.MyQTriggers.MyValueTaskOfTStaticAsyncMethod", StringComparison.Ordinal))
            {
                context.GetInvocationResult().Value = await global::FunctionApp26.MyQTriggers.MyValueTaskOfTStaticAsyncMethod((string)inputArguments[0]);
            }
            else if (string.Equals(context.FunctionDefinition.EntryPoint, "FunctionApp26.MyQTriggers.MyValueTaskStaticAsyncMethod2", StringComparison.Ordinal))
            {
                await global::FunctionApp26.MyQTriggers.MyValueTaskStaticAsyncMethod2((string)inputArguments[0]);
            }
            else if (string.Equals(context.FunctionDefinition.EntryPoint, "FunctionApp26.BlobTriggers.Run", StringComparison.Ordinal))
            {
                await global::FunctionApp26.BlobTriggers.Run((global::System.IO.Stream)inputArguments[0], (string)inputArguments[1]);
            }
            else if (string.Equals(context.FunctionDefinition.EntryPoint, "FunctionApp26.EventHubTriggers.Run1", StringComparison.Ordinal))
            {
                global::FunctionApp26.EventHubTriggers.Run1((global::Azure.Messaging.EventHubs.EventData[])inputArguments[0]);
            }
            else if (string.Equals(context.FunctionDefinition.EntryPoint, "FunctionApp26.EventHubTriggers.Run2", StringComparison.Ordinal))
            {
                context.GetInvocationResult().Value = global::FunctionApp26.EventHubTriggers.Run2((global::Azure.Messaging.EventHubs.EventData)inputArguments[0]);
            }
            else if (string.Equals(context.FunctionDefinition.EntryPoint, "FunctionApp26.EventHubTriggers.RunAsync1", StringComparison.Ordinal))
            {
                await global::FunctionApp26.EventHubTriggers.RunAsync1((global::Azure.Messaging.EventHubs.EventData[])inputArguments[0]);
            }
            else if (string.Equals(context.FunctionDefinition.EntryPoint, "FunctionApp26.EventHubTriggers.RunAsync2", StringComparison.Ordinal))
            {
                await global::FunctionApp26.EventHubTriggers.RunAsync2((global::Azure.Messaging.EventHubs.EventData[])inputArguments[0]);
            }
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
                s.AddSingleton<global::Microsoft.Azure.Functions.Worker.Invocation.IFunctionExecutor, DirectFunctionExecutor>();
            });
        }
    }
}