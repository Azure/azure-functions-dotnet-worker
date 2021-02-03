// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Logging;
using Microsoft.Azure.Functions.Worker.Pipeline;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    internal sealed class ConverterMiddleware
    {
        private readonly IEnumerable<IConverter> _converters;
        private readonly FunctionsHostOutputChannel _channel;

        public ConverterMiddleware(IEnumerable<IConverter> converters, FunctionsHostOutputChannel channel)
        {
            _converters = converters;
            _channel = channel;
        }

        public Task Invoke(FunctionExecutionContext context, FunctionExecutionDelegate next)
        {
            foreach (var param in context.FunctionDefinition.Parameters)
            {
                if (param.Type == typeof(FunctionExecutionContext))
                {
                    context.Logger = new InvocationLogger(context.Invocation.InvocationId, _channel.Channel.Writer);
                    param.Value = context;
                }
                else
                {
                    object? source = context.Invocation.ValueProvider.GetValue(param.Name);
                    var converterContext = new DefaultConverterContext(param, source, context);
                    if (TryConvert(converterContext, out object? target))
                    {
                        param.Value = target;
                        continue;
                    }
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
