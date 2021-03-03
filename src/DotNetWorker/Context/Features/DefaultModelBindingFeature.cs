// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Definition;

namespace Microsoft.Azure.Functions.Worker.Context.Features
{
    internal class DefaultModelBindingFeature : IModelBindingFeature
    {
        private readonly IEnumerable<IConverter> _converters;
        private bool _inputBound;
        private object?[]? _parameterValues;

        public DefaultModelBindingFeature(IEnumerable<IConverter> converters)
        {
            _converters = converters ?? throw new ArgumentNullException(nameof(converters));
        }

        public object?[]? InputArguments => _parameterValues;

        public object?[] BindFunctionInput(FunctionContext context)
        {
            if (_inputBound)
            {
                throw new InvalidOperationException("Duplicate binding call detected. " +
                    $"Input parameters can only be bound to arguments once. Use the {nameof(InputArguments)} property to inspect values.");
            }

            _parameterValues = new object?[context.FunctionDefinition.Parameters.Length];
            _inputBound = true;

            for (int i = 0; i < _parameterValues.Length; i++)
            {
                FunctionParameter param = context.FunctionDefinition.Parameters[i];

                var functionBindings = context.Features.Get<IFunctionBindingsFeature>();

                object? source = null;

                // Check InputData first, then TriggerMetadata
                if (functionBindings is not null)
                {
                    if (!functionBindings.InputData.TryGetValue(param.Name, out source))
                    {
                        functionBindings.TriggerMetadata.TryGetValue(param.Name, out source);
                    }
                }

                var converterContext = new DefaultConverterContext(param, source, context);

                if (TryConvert(converterContext, out object? target))
                {
                    _parameterValues[i] = target;
                    continue;
                }
            }

            return _parameterValues;
        }

        internal bool TryConvert(ConverterContext context, out object? target)
        {
            target = null;

            // The first converter to successfully convert wins.
            // For example, this allows a converter that parses JSON strings to return false if the
            // string is not valid JSON. This manager will then continue with the next matching provider.
            foreach (var converter in _converters)
            {
                if (converter.TryConvert(context, out object? targetObj))
                {
                    target = targetObj;
                    return true;
                }
            }

            return false;
        }
    }
}
