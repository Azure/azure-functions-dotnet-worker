// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Diagnostics.Exceptions;

namespace Microsoft.Azure.Functions.Worker.Context.Features
{
    internal class DefaultModelBindingFeature : IModelBindingFeature
    {
        private bool _inputBound;
        private object?[]? _parameterValues;
        private readonly IConverterContextFactory _converterContextFactory;

        public DefaultModelBindingFeature(IConverterContextFactory converterContextFactory)
        {
            _converterContextFactory = converterContextFactory ??
                                       throw new ArgumentNullException(nameof(converterContextFactory));
        }

        public object?[]? InputArguments => _parameterValues;

        public async ValueTask<object?[]> BindFunctionInputAsync(FunctionContext context)
        {
            if (_inputBound)
            {
                throw new InvalidOperationException("Duplicate binding call detected. " +
                    $"Input parameters can only be bound to arguments once. Use the {nameof(InputArguments)} property to inspect values.");
            }

            _parameterValues = new object?[context.FunctionDefinition.Parameters.Length];
            _inputBound = true;

            var inputConversionFeature = context.Features.Get<IInputConversionFeature>();

            if (inputConversionFeature == null)
            {
                throw new InvalidOperationException("Input conversion feature is missing.");
            }

            List<string>? errors = null;
            for (int i = 0; i < _parameterValues.Length; i++)
            {
                FunctionParameter param = context.FunctionDefinition.Parameters[i];

                IFunctionBindingsFeature functionBindings = context.GetBindings();

                // Check InputData first, then TriggerMetadata
                if (!functionBindings.InputData.TryGetValue(param.Name, out object? source))
                {
                    functionBindings.TriggerMetadata.TryGetValue(param.Name, out source);
                }

                IReadOnlyDictionary<string, object> properties = ImmutableDictionary<string, object>.Empty;

                // Pass info about specific input converter type defined for this parameter, if present.
                if (param.Properties.TryGetValue(PropertyBagKeys.ConverterType, out var converterTypeAssemblyFullName))
                {
                    properties = new Dictionary<string, object>()
                    {
                        { PropertyBagKeys.ConverterType, converterTypeAssemblyFullName }
                    };
                }

                var converterContext = _converterContextFactory.Create(param.Type, source, context, properties);
                
                var bindingResult = await inputConversionFeature.ConvertAsync(converterContext);

                if (bindingResult.Status == ConversionStatus.Succeeded)
                {
                    _parameterValues[i] = bindingResult.Value;
                }
                else if (bindingResult.Status == ConversionStatus.Failed && source is not null)
                {
                    // Don't initialize this list unless we have to
                    errors ??= new List<string>();

                    errors.Add(
                        $"Cannot convert input parameter '{param.Name}' to type '{param.Type.FullName}' from type '{source.GetType().FullName}'. Error:{bindingResult.Error}");
                }
            }

            // found errors
            if (errors is not null)
            {
                throw new FunctionInputConverterException(
                    $"Error converting {errors.Count} input parameters for Function '{context.FunctionDefinition.Name}': {string.Join(Environment.NewLine, errors)}");
            }

            return _parameterValues;
        }
    }
}
