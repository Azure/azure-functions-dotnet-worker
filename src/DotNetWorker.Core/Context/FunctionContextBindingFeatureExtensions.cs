// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// FunctionContext extension methods for binding data.
    /// </summary>
    public static class FunctionContextBindingFeatureExtensions
    {
        /// <summary>
        /// Binds an input binding item for the requested type.
        /// </summary>
        /// <typeparam name="T">The type of input item to bind to.</typeparam>
        /// <param name="context">The function context.</param>
        /// <returns>A <see cref="Task"/> wrapping an instance of T if binding was successful, else null</returns>
        /// <exception cref="InvalidOperationException">Throws when more than one input binding entry found for the requested type.</exception>
        public static async Task<T?> BindInputAsync<T>(this FunctionContext context)
        {
            var requestedInputBindingType = typeof(T);

            // find the parameter from function definition for the Type requested.
            FunctionParameter? parameter = null;
            foreach (var param in context.FunctionDefinition.Parameters)
            {
                if (param.Type.IsAssignableFrom(requestedInputBindingType))
                {
                    if (parameter != null)
                    {
                        // More than one parameter found with the type requested.
                        // customer should use the other overload of this method with an explicit BindingMetadata instance.
                        throw new InvalidOperationException("More than one input binding item found for the requested type. Use the BindInputAsync overload which takes an instance of BindingMetadata");
                    }
                    parameter = param;
                }
            }

            if (parameter != null)
            {
                return await GetConvertedValueFromInputConversionFeature<T>(context, parameter);
            }

            return default;
        }

        /// <summary>
        /// Binds an input binding item for the requested <see cref="BindingMetadata"/> instance.
        /// </summary>
        /// <param name="context">The function context.</param>
        /// <param name="bindingMetadata">The BindingMetadata instance for which input data should bound to.</param>
        /// <returns>A <see cref="Task"/> wrapping an instance of T if binding was successful, else null</returns>
        public static async Task<T?> BindInputAsync<T>(this FunctionContext context, BindingMetadata bindingMetadata)
        {
            if (bindingMetadata == null)
            {
                throw new ArgumentNullException(nameof(bindingMetadata));
            }

            // find the parameter from function definition for the bindingMetadata requested.
            FunctionParameter? parameter = null;
            foreach (var param in context.FunctionDefinition.Parameters)
            {
                if (param.Name == bindingMetadata.Name)
                {
                    parameter = param;
                    break;
                }
            }

            if (parameter != null)
            {
                return await GetConvertedValueFromInputConversionFeature<T>(context, parameter);
            }

            return default;
        }

        /// <summary>
        /// Gets the invocation result of the current function invocation.
        /// </summary>
        /// <param name="context">The function context instance.</param>
        /// <returns>An instance of <see cref="InvocationResult{T}"/>.</returns>
        /// <exception cref="InvalidOperationException">Throws when the invocation result is not of the requested type.</exception>
        public static InvocationResult<T> GetInvocationResult<T>(this FunctionContext context)
        {
            if (context.GetBindings().InvocationResult is T resultAsT)
            {
                return new InvocationResult<T>(context, resultAsT);
            }

            throw new InvalidOperationException("Invocation result is not of the requested type. Consider using the overload which does not specify the type.");
        }

        /// <summary>
        /// Gets the invocation result of the current function invocation.
        /// </summary>
        /// <param name="context">The function context instance.</param>
        /// <returns>An instance of <see cref="InvocationResult"/>.</returns>
        public static InvocationResult GetInvocationResult(this FunctionContext context)
        {
            var invocationResult = context.GetBindings().InvocationResult;

            return new InvocationResult(context, invocationResult);
        }

        /// <summary>
        /// Gets the output binding entries for the current function invocation.
        /// </summary>
        /// <param name="context">The function context instance.</param>
        /// <returns>Collection of OutputBindingData instances of the requested type T.</returns>
        public static IEnumerable<OutputBindingData<T>> GetOutputBindings<T>(this FunctionContext context)
        {
            var bindingsFeature = context.GetBindings();

            foreach (var data in bindingsFeature.OutputBindingData)
            {
                if (data.Value is T valueAsT)
                {
                    // Gets binding type (blob,queue etc) from function definition.
                    string? bindingType = null;
                    if (context.FunctionDefinition.OutputBindings.TryGetValue(data.Key, out var bindingData))
                    {
                        bindingType = bindingData.Type;
                    }

                    yield return new OutputBindingData<T>(context, data.Key, valueAsT, bindingType);
                }
            }
        }

        /// <summary>
        /// Executes the input conversion feature to bind the value of the parameter.
        /// </summary>
        private static async Task<T?> GetConvertedValueFromInputConversionFeature<T>(FunctionContext context, FunctionParameter parameter)
        {
            var converterContextFactory = context.InstanceServices.GetService<IConverterContextFactory>();
            var inputConversionFeature = context.Features.Get<IInputConversionFeature>();
            var functionBindings = context.GetBindings();

            // Check InputData first, then TriggerMetadata
            if (!functionBindings.InputData.TryGetValue(parameter.Name, out object? source))
            {
                functionBindings.TriggerMetadata.TryGetValue(parameter.Name, out source);
            }

            var converterContext = converterContextFactory!.Create(parameter.Type, source, context);
            var bindingResult = await inputConversionFeature!.ConvertAsync(converterContext);

            if (bindingResult.Status == ConversionStatus.Succeeded && bindingResult.Value!=null)
            {
                return (T)bindingResult.Value;
            }

            return default;
        }
    }
}
