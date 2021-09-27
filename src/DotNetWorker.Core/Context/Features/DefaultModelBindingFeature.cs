// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Core.Converters;
using Microsoft.Azure.Functions.Worker.Diagnostics.Exceptions;

namespace Microsoft.Azure.Functions.Worker.Context.Features
{
    internal class DefaultModelBindingFeature : IModelBindingFeature
    {
        private bool _inputBound;
        private object?[]? _parameterValues;

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
                            
            IInputConversionFeature conversionFeature = context.Features.Get<IInputConversionFeature>();

            if (conversionFeature == null)
            {                                
                throw new InvalidOperationException("Conversion feature is missing!");
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

                var converterContext = new DefaultConverterContext(param.Type, source, context);

                if (param.BindingConverterType != null)
                {
                    converterContext.Properties = new Dictionary<string, object>
                    {
                        { PropertyBagKeys.ConverterType, param.BindingConverterType}
                    };
                }

                var bindingResult = await conversionFeature!.ConvertAsync(converterContext);

                if (bindingResult.IsSuccess)
                {
                    _parameterValues[i] = bindingResult.Model;
                }
                else if (source is not null)
                {
                    // Don't initialize this list unless we have to
                    if (errors is null)
                    {
                        errors = new List<string>();
                    }

                    errors.Add($"Cannot convert input parameter '{param.Name}' to type '{param.Type.FullName}' from type '{source.GetType().FullName}'.");
                }
            }

            // found errors
            if (errors is not null)
            {
                throw new FunctionInputConverterException($"Error converting {errors.Count} input parameters for Function '{context.FunctionDefinition.Name}': {string.Join(Environment.NewLine, errors)}");
            }

            return _parameterValues;
        }
    }
}
