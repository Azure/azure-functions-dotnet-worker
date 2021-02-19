// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    internal class MemoryConverter : IConverter
    {
        public bool TryConvert(ConverterContext context, out object? target)
        {
            target = default;

            if (context.Source is not ReadOnlyMemory<byte> sourceMemory)
            {
                return false;
            }

            if (context.Parameter.Type.IsAssignableFrom(typeof(string)))
            {
                target = Encoding.UTF8.GetString(sourceMemory.Span);
                return true;
            }

            if (context.Parameter.Type.IsAssignableFrom(typeof(byte[])))
            {
                target = sourceMemory.ToArray();
                return true;
            }

            return false;
        }
    }
}
