﻿using System;
using Microsoft.Azure.Functions.Worker.Definition;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    internal class OutputBindingConverter : IConverter
    {
        internal static Type defaultBindingType = typeof(DefaultOutputBinding<>);

        public bool TryConvert(ConverterContext context, out object? target)
        {
            FunctionParameter param = context.Parameter;
            target = null;

            if (param.Type.IsGenericType)
            {
                var genericType = param.Type.GetGenericTypeDefinition();

                if (genericType == typeof(OutputBinding<>))
                {
                    var elementType = param.Type.GetGenericArguments()[0];
                    Type constructed = defaultBindingType.MakeGenericType(new Type[] { elementType });
                    target = Activator.CreateInstance(constructed, context.Parameter, context.ExecutionContext.OutputBindings);
                    return true;
                }
            }

            return false;
        }
    }
}
