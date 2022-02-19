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
        /// Gets the input binding data for the current function invocation.
        /// </summary>
        /// <param name="context">The function context instance.</param>
        /// <returns>The input binding data as a read only dictionary.</returns>
        public static IReadOnlyDictionary<string, object?> GetInputData(this FunctionContext context)
        {
            return context.GetBindings().InputData;
        }

        /// <summary>
        /// Gets the trigger meta data for the current function invocation.
        /// </summary>
        /// <param name="context">The function context instance.</param>
        /// <returns>The invocation trigger meta data as a read only dictionary.</returns>
        public static IReadOnlyDictionary<string, object?> GetTriggerMetadata(this FunctionContext context)
        {
            return context.GetBindings().TriggerMetadata;
        }

        /// <summary>
        /// Binds an input item for the requested type.
        /// </summary>
        /// <typeparam name="T">The type of input item to bind to.</typeparam>
        /// <param name="context">The function context.</param>
        /// <returns>An instance of <see cref="T"/> if binding was successful, else null</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static async Task<T?> BindInputAsync<T>(this FunctionContext context)
        {
            var inputType = typeof(T);

            // find the parameter from function definition for the Type requested.
            // Use that parameter definition(which has Type) to get converted value.

            FunctionParameter? parameter = null;
            foreach (var param in context.FunctionDefinition.Parameters)
            {
                if (param.Type.IsAssignableFrom(inputType))
                {
                    if (parameter != null)
                    {
                        // More than one parameter found with the type requested.
                        // customer should use the other overload of this method with an explicit FunctionParameter instance.
                        throw new InvalidOperationException("More than one binding item found for the requested Type. Use the BindInput overload which takes an instance of FunctionParameter");
                    }
                    parameter = param;
                }
            }

            return await BindInputAsync<T>(context, parameter);
        }

        /// <summary>
        /// Binds an input item for the requested function parameter.
        /// </summary>
        /// <param name="context">The function context.</param>
        /// <param name="parameter">The function parameter for which input data should bound to.</param>
        /// <returns></returns>
        public static async Task<T?> BindInputAsync<T>(this FunctionContext context, FunctionParameter parameter)
        {
            if (parameter != null)
            {
                var convertedValue = await GetConvertedValueFromInputConversionFeature(context, parameter);

                return (T)convertedValue;
            }

            return default;
        }

        /// <summary>
        /// Gets the invocation result of the current function invocation.
        /// </summary>
        /// <param name="context">The function context instance.</param>
        /// <returns>The invocation result value.</returns>
        public static InvocationResult<T>? GetInvocationResult<T>(this FunctionContext context)
        {
            if (context.GetBindings().InvocationResult is T t)
            {
                return new InvocationResult<T>(context, t);
            }

            return default;
        }

        /// <summary>
        /// Gets the output binding entries for the current function invocation.
        /// </summary>
        /// <param name="context">The function context instance.</param>
        /// <returns>Collection of <see cref="OutputBindingData"/></returns>
        public static IEnumerable<OutputBindingData> GetOutputBindings(this FunctionContext context)
        {
            var bindingsFeature = context.GetBindings();

            foreach (var data in bindingsFeature.OutputBindingData)
            {
                // Gets binding type (http,queue etc) from function definition.
                string? bindingType = null;
                if (context.FunctionDefinition.OutputBindings.TryGetValue(data.Key, out var bindingData))
                {
                    bindingType = bindingData.Type;
                }

                yield return new OutputBindingData(context, data.Key, data.Value, bindingType);
            }
        }

        /// <summary>
        /// Executes the input conversion feature to bind the value of the parameter.
        /// </summary>
        private static async Task<object?> GetConvertedValueFromInputConversionFeature(FunctionContext context, FunctionParameter parameter)
        {
            IFunctionBindingsFeature functionBindings = context.GetBindings();

            var converterContextFactory = context.InstanceServices.GetService<IConverterContextFactory>();

            var inputConversionFeature = context.Features.Get<IInputConversionFeature>();

            // Check InputData first, then TriggerMetadata
            if (!functionBindings.InputData.TryGetValue(parameter.Name, out object? source))
            {
                functionBindings.TriggerMetadata.TryGetValue(parameter.Name, out source);
            }

            var converterContext = converterContextFactory!.Create(parameter.Type, source, context);
            var bindingResult = await inputConversionFeature!.ConvertAsync(converterContext);

            return bindingResult.Value;
        }
    }
}
