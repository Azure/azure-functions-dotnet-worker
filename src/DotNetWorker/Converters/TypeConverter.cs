// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    internal class TypeConverter : IConverter
    {
        public bool TryConvert(ConverterContext context, out object? target)
        {
            Type? sourceType = context.Source?.GetType();
            if (sourceType is not null &&
                context.Parameter.Type.IsAssignableFrom(sourceType))
            {
                target = context.Source;
                return true;
            }

            context.Parameter.Type.IsAssignableFrom(context.Source?.GetType());

            // Special handling for the context.
            if (context.Parameter.Type == typeof(FunctionExecutionContext))
            {
                target = context.ExecutionContext;
                return true;
            }

            target = default;
            return false;
        }
    }
}
