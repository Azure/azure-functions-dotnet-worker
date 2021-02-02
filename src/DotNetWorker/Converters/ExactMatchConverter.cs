// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.Converters
{
    internal class ExactMatchConverter : IConverter
    {
        public bool TryConvert(ConverterContext context, out object? target)
        {
            if (context.Source.GetType() == context.Parameter.Type)
            {
                target = context.Source;
                return true;
            }

            target = default;
            return false;
        }
    }
}
