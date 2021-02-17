// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Pipeline;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    internal sealed class ConverterMiddleware
    {
        private readonly IEnumerable<IConverter> _converters;

        public ConverterMiddleware(IEnumerable<IConverter> converters)
        {
            _converters = converters ?? throw new ArgumentNullException(nameof(converters));
        }

        public Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            // TODO: The value needs to be moved to the context
            // parameters values should be properly associated with the context
            // and support disposal.
            foreach (var param in context.FunctionDefinition.Parameters)
            {
                object? source = context.Invocation.ValueProvider.GetValue(param.Name);
                var converterContext = new DefaultConverterContext(param, source, context);
                if (TryConvert(converterContext, out object? target))
                {
                    param.Value = target;
                    continue;
                }
            }

            return next(context);
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
