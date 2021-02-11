// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    internal class ExactMatchConverter : IConverter
    {
        public bool TryConvert(ConverterContext context, out object? target)
        {
            if (IsSameOrExtensionOf(context.Source?.GetType(), context.Parameter.Type))
            {
                target = context.Source;
                return true;
            }

            target = default;
            return false;
        }

        private static bool IsSameOrExtensionOf(Type? A, Type B)
        {
            return A == B
                || (A?.IsSubclassOf(B) ?? false)
                || (A?.GetInterfaces().Any(i => i == B) ?? false);
        }
    }
}
