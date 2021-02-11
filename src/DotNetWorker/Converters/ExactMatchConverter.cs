// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    internal class ExactMatchConverter : IConverter
    {
        public bool TryConvert(ConverterContext context, out object? target)
        {
            if (IsSameOrSubclassOf(context.Source?.GetType(), context.Parameter.Type))
            {
                target = context.Source;
                return true;
            }

            target = default;
            return false;
        }

        private static bool IsSameOrSubclassOf(Type? A, Type B)
        {
            return A == B || (A?.IsSubclassOf(B) ?? false);
        }
    }
}
