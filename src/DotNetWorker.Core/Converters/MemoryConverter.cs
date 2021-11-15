// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    internal class MemoryConverter : IInputConverter
    {
        public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            if (context.Source is not ReadOnlyMemory<byte> sourceMemory)
            {
                return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
            }

            if (context.TargetType.IsAssignableFrom(typeof(string)))
            {
                var target = Encoding.UTF8.GetString(sourceMemory.Span);
                return new ValueTask<ConversionResult>(ConversionResult.Success(target));
            }

            if (context.TargetType.IsAssignableFrom(typeof(byte[])))
            {
                var target = sourceMemory.ToArray();
                return new ValueTask<ConversionResult>(ConversionResult.Success(target));
            }

            return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
        }
    }
}
