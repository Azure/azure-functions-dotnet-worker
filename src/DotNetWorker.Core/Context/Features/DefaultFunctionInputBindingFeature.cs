// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.Functions.Worker.Context.Features
{
    internal class DefaultFunctionInputBindingFeature : IFunctionInputBindingFeature, IDisposable
    {
        private const int WaitTimeInMilliSeconds = 100;
        private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);
        private readonly IConverterContextFactory _converterContextFactory;
        private FunctionInputBindingResult? _inputBindingResult;
        private bool _disposed;

        public DefaultFunctionInputBindingFeature(IConverterContextFactory converterContextFactory)
        {
            _converterContextFactory = converterContextFactory
                                     ?? throw new ArgumentNullException(nameof(converterContextFactory));
        }

        public async ValueTask<FunctionInputBindingResult> BindFunctionInputAsync(FunctionContext context)
        {
            ObjectDisposedThrowHelper.ThrowIf(_disposed, this);

            // Setting second parameter to CancellationToken.None to prevent a TaskCancelledException if the 
            // pipeline cancels the code before the function is invoked. This way the customer code will be invoked
            // and they can handle the cancellation token.
            await _semaphoreSlim.WaitAsync(WaitTimeInMilliSeconds, CancellationToken.None);

            try
            {
                if (_inputBindingResult is not null)
                {
                    // Return the cached value if BindFunctionInputAsync is called a second time during invocation.
                    return _inputBindingResult!;
                }

                IFunctionBindingsFeature functionBindings = context.GetBindings();
                var inputConversionFeature = context.Features.Get<IInputConversionFeature>()
                    ?? throw new InvalidOperationException("Input conversion feature is missing.");

                var parameterValues = new object?[context.FunctionDefinition.Parameters.Length];
                List<string>? errors = null;

                for (int i = 0; i < parameterValues.Length; i++)
                {
                    FunctionParameter parameter = context.FunctionDefinition.Parameters[i];

                    TryGetBindingSource(parameter.Name, functionBindings, out object? source);

                    // Option 1: Check if source is an empty string and parameter is reference or nullable type
                    if (source is string str && string.IsNullOrEmpty(str))
                    {
                        // OR we can set `parameterValues[i] = null`. However, we already have checks for HasDefaultValue & IsReferenceOrNullableType
                        // in the `(bindingResult.Status == ConversionStatus.Unhandled)` case below which will hanlde what we need it to.
                        // So it depends on if we want to short circuit or not.
                        source = null;
                    }

                    ConversionResult bindingResult = await ConvertAsync(context, parameter, _converterContextFactory, inputConversionFeature, source);


                    if (bindingResult.Status == ConversionStatus.Succeeded)
                    {
                        parameterValues[i] = bindingResult.Value;
                    }
                    else if (bindingResult.Status == ConversionStatus.Failed && source is not null)
                    {
                        // Don't initialize this list unless we have to
                        errors ??= new List<string>();

                        errors.Add($"Cannot convert input parameter '{parameter.Name}' to type '{parameter.Type.FullName}' from type '{source.GetType().FullName}'. Error:{bindingResult.Error}");
                    }
                    else if (bindingResult.Status == ConversionStatus.Unhandled)
                    {
                        // If still unhandled after going through all converters,check an explicit default value was provided for the parameter.
                        if (parameter.HasDefaultValue)
                        {
                            parameterValues[i] = parameter.DefaultValue;
                        }
                        else if (parameter.IsReferenceOrNullableType)
                        {
                            // If the parameter is a reference type or nullable type, set it to null.
                            parameterValues[i] = null;
                        }
                        else
                        {
                            // Non nullable value type with no default value.
                            // We could not find a value for this param. should throw.
                            errors ??= new List<string>();
                            errors.Add(
                                $"Could not populate the value for '{parameter.Name}' parameter. Consider adding a default value or making the parameter nullable.");
                        }
                    }
                }

                // found errors
                if (errors is not null)
                {
                    throw new FunctionInputConverterException(
                        $"Error converting {errors.Count} input parameters for Function '{context.FunctionDefinition.Name}': {string.Join(Environment.NewLine, errors)}");
                }

                _inputBindingResult = new FunctionInputBindingResult(parameterValues);

                return _inputBindingResult;
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        internal static bool TryGetBindingSource(string bindingName, FunctionContext context, out object? source)
            => TryGetBindingSource(bindingName, context.GetBindings(), out source);

        internal static ValueTask<ConversionResult> ConvertAsync(FunctionContext context, FunctionParameter parameter)
        {
            var converterContextFactory = context.InstanceServices.GetRequiredService<IConverterContextFactory>();
            var inputConversionFeature = context.Features.Get<IInputConversionFeature>()
                ?? throw new InvalidOperationException($"The {nameof(IInputConversionFeature)} is not available in the current context.");

            TryGetBindingSource(parameter.Name, context, out object? source);

            return ConvertAsync(context, parameter, converterContextFactory, inputConversionFeature, source);
        }

        private static bool TryGetBindingSource(string bindingName, IFunctionBindingsFeature functionBindingsFeature, out object? source)
        {
            // Check InputData first, then TriggerMetadata
            if (!functionBindingsFeature.InputData.TryGetValue(bindingName, out source))
            {
                functionBindingsFeature.TriggerMetadata.TryGetValue(bindingName, out source);
            }

            return source is not null;
        }

        private static async ValueTask<ConversionResult> ConvertAsync(FunctionContext context, FunctionParameter parameter,
            IConverterContextFactory converterContextFactory, IInputConversionFeature inputConversionFeature, object? source)
        {
            var inputBindingCache = context.InstanceServices.GetService<IBindingCache<ConversionResult>>();


            var cacheKey = parameter.Name;
            if (inputBindingCache!.TryGetValue(cacheKey, out var cachedResult))
            {
                return cachedResult;
            }
            else
            {
                var properties = new Dictionary<string, object>();

                AddFunctionParameterPropertyIfPresent(properties, parameter, PropertyBagKeys.ConverterType);
                AddFunctionParameterPropertyIfPresent(properties, parameter, PropertyBagKeys.ConverterFallbackBehavior);
                AddFunctionParameterPropertyIfPresent(properties, parameter, PropertyBagKeys.BindingAttributeSupportedConverters);
                AddFunctionParameterPropertyIfPresent(properties, parameter, PropertyBagKeys.BindingAttribute);

                var converterContext = converterContextFactory.Create(parameter.Type, source, context, properties.Count != 0
                                     ? properties.ToImmutableDictionary()
                                     : ImmutableDictionary<string, object>.Empty);

                var result = await inputConversionFeature.ConvertAsync(converterContext);
                inputBindingCache[cacheKey] = result;

                return result;
            }
        }

        private static void AddFunctionParameterPropertyIfPresent(IDictionary<string, object> properties, FunctionParameter param, string key)
        {
            if (param.Properties.TryGetValue(key, out object? val))
            {
                properties.Add(key, val);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _semaphoreSlim.Dispose();
        }
    }
}
