// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.Converters
{
    internal class FunctionContextConverter : IConverter
    {
        public bool TryConvert(ConverterContext context, out object? target)
        {
            target = null;

            // Special handling for the context.
            if (context.Parameter.Type == typeof(FunctionContext))
            {
                target = context.FunctionContext;
                return true;
            }

            return false;
        }
    }
}
