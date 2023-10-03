// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
        /// Binds an input binding item for the requested <see cref="BindingMetadata"/> instance.
        /// </summary>
        /// <param name="context">The function context.</param>
        /// <param name="bindingMetadata">The BindingMetadata instance for which input data should bound to.</param>
        /// <returns>An instance of <see cref="InputBindingData{T}"/> which wraps the input binding operation result value.</returns>
        public static async ValueTask<InputBindingData<T>> BindInputAsync<T>(this FunctionContext context, BindingMetadata bindingMetadata)
        {
            if (bindingMetadata == null)
            {
                throw new ArgumentNullException(nameof(bindingMetadata));
            }

            var bindingCache = context.InstanceServices.GetService<IBindingCache<ConversionResult>>()!;

            if (!bindingCache!.TryGetValue(bindingMetadata.Name, out ConversionResult bindingResult))
            {
                bindingResult = await GetConvertedValueFromInputConversionFeature(context, bindingMetadata, typeof(T));
                bindingCache.TryAdd(bindingMetadata.Name, bindingResult);
            }

            return new DefaultInputBindingData<T>(bindingCache, bindingMetadata, (T?)bindingResult.Value);
        }

        /// <summary>
        /// Gets the invocation result of the current function invocation.
        /// </summary>
        /// <param name="context">The function context instance.</param>
        /// <returns>An instance of <see cref="InvocationResult{T}"/>.</returns>
        /// <exception cref="InvalidOperationException">Throws when the invocation result is not of the requested type.</exception>
        public static InvocationResult<T> GetInvocationResult<T>(this FunctionContext context)
        {
            var invocationResult = context.GetBindings().InvocationResult;
            if (invocationResult is T result)
            {
                return new DefaultInvocationResult<T>(context, result);
            }

            throw new InvalidOperationException($"Requested type({typeof(T)}) does not match the type of Invocation result({invocationResult!.GetType()})");
        }

        /// <summary>
        /// Gets the invocation result of the current function invocation.
        /// </summary>
        /// <param name="context">The function context instance.</param>
        /// <returns>An instance of <see cref="InvocationResult"/>.</returns>
        public static InvocationResult GetInvocationResult(this FunctionContext context)
        {
            var invocationResult = context.GetBindings().InvocationResult;

            return new DefaultInvocationResult(context, invocationResult);
        }

        /// <summary>
        /// Gets the output binding entries for the current function invocation.
        /// </summary>
        /// <param name="context">The function context instance.</param>
        /// <returns>Collection of OutputBindingData instances where the Value is converted to T type.</returns>
        public static IEnumerable<OutputBindingData<T>> GetOutputBindings<T>(this FunctionContext context)
        {
            var bindingsFeature = context.GetBindings();

            foreach (var binding in context.FunctionDefinition.OutputBindings)
            {
                T? itemValue = default;
                if (bindingsFeature.OutputBindingData.TryGetValue(binding.Key, out var val) && val is T valueAsT)
                {
                    itemValue = valueAsT;
                }

                // Gets binding type (ex: blob,queue) from function definition.
                string bindingType = binding.Value.Type;

                yield return new DefaultOutputBindingData<T>(context, binding.Key, itemValue, bindingType);
            }
        }

        /// <summary>
        /// Executes the input conversion feature to bind the value of the parameter.
        /// </summary>
        private static async ValueTask<ConversionResult> GetConvertedValueFromInputConversionFeature(FunctionContext context, BindingMetadata bindingMetadata, Type targetType)
        {
            DefaultFunctionInputBindingFeature.TryGetBindingSource(bindingMetadata.Name, context, out object? source);

            var converterContextFactory = context.InstanceServices.GetService<IConverterContextFactory>();
            var inputConversionFeature = context.Features.Get<IInputConversionFeature>();
            var converterContext = converterContextFactory!.Create(targetType, source, context, ImmutableDictionary<string, object>.Empty);

            ConversionResult conversionResult = await inputConversionFeature!.ConvertAsync(converterContext);

            if (conversionResult.Status != ConversionStatus.Unhandled)
            {
                return conversionResult;
            }

            // Fallback to using parameter defined converters.
            FunctionParameter? parameter = context.FunctionDefinition.Parameters
            .FirstOrDefault(p => string.Equals(p.Name, bindingMetadata.Name, StringComparison.OrdinalIgnoreCase));

            if (parameter is not null)
            {
                return await DefaultFunctionInputBindingFeature.ConvertAsync(context, parameter);
            }

            return ConversionResult.Unhandled();
        }
    }
}
