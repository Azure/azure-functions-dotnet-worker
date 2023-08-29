// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
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

            await _semaphoreSlim.WaitAsync(WaitTimeInMilliSeconds, context.CancellationToken);

            try
            {
                if (_inputBindingResult is not null)
                {
                    // Return the cached value if BindFunctionInputAsync is called a second time during invocation.
                    return _inputBindingResult!;
                }

                IFunctionBindingsFeature functionBindings = context.GetBindings();
                var inputBindingCache = context.InstanceServices.GetService<IBindingCache<ConversionResult>>();
                var inputConversionFeature = context.Features.Get<IInputConversionFeature>();

                if (inputConversionFeature == null)
                {
                    throw new InvalidOperationException("Input conversion feature is missing.");
                }

                var parameterValues = new object?[context.FunctionDefinition.Parameters.Length];
                List<string>? errors = null;

                for (int i = 0; i < parameterValues.Length; i++)
                {
                    FunctionParameter param = context.FunctionDefinition.Parameters[i];

                    // Check InputData first, then TriggerMetadata
                    if (!functionBindings.InputData.TryGetValue(param.Name, out object? source))
                    {
                        functionBindings.TriggerMetadata.TryGetValue(param.Name, out source);
                    }

                    ConversionResult bindingResult;
                    var cacheKey = param.Name;
                    if (inputBindingCache!.TryGetValue(param.Name, out var cachedResult))
                    {
                        bindingResult = cachedResult;
                    }
                    else
                    {
                        var properties = new Dictionary<string, object>();

                        AddFunctionParameterPropertyIfPresent(properties, param, PropertyBagKeys.ConverterType);
                        AddFunctionParameterPropertyIfPresent(properties, param, PropertyBagKeys.ConverterFallbackBehavior);
                        AddFunctionParameterPropertyIfPresent(properties, param, PropertyBagKeys.BindingAttributeSupportedConverters);
                        AddFunctionParameterPropertyIfPresent(properties, param, PropertyBagKeys.BindingAttribute);

                        var converterContext = _converterContextFactory.Create(param.Type, source, context, properties.Count() != 0
                                             ? properties.ToImmutableDictionary()
                                             : ImmutableDictionary<string, object>.Empty);

                        bindingResult = await inputConversionFeature.ConvertAsync(converterContext);
                        inputBindingCache[cacheKey] = bindingResult;
                    }

                    if (bindingResult.Status == ConversionStatus.Succeeded)
                    {
                        parameterValues[i] = bindingResult.Value;
                    }
                    else if (bindingResult.Status == ConversionStatus.Failed && source is not null)
                    {
                        // Don't initialize this list unless we have to
                        errors ??= new List<string>();

                        errors.Add(
                            $"Cannot convert input parameter '{param.Name}' to type '{param.Type.FullName}' from type '{source.GetType().FullName}'. Error:{bindingResult.Error}");
                    }
                    else if (bindingResult.Status == ConversionStatus.Unhandled)
                    {
                        // If still unhandled after going through all converters,check an explicit default value was provided for the function parameter.
                        if (param.DefaultValue is not null)
                        {
                            parameterValues[i] = param.DefaultValue;
                        }
                        else
                        {
                            // We could not find a value for this param. should throw.
                            errors ??= new List<string>();

                            errors.Add(
                                $"Could not populate the value for '{param.Name}' parameter. Consider updating the parameter with an default value.");
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

        private void AddFunctionParameterPropertyIfPresent(IDictionary<string, object> properties, FunctionParameter param, string key)
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
