// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    // Converting IEnumerable<> to Array
    internal class ArrayConverter : IConverter
    {
        // Convert IEnumerable to array
        public bool TryConvert(ConverterContext context, out object? target)
        {
            target = null;
            // Ensure requested type is an array
            if (context.Parameter.Type.IsArray)
            {
                Type? elementType = context.Parameter.Type.GetElementType();
                if (elementType is not null)
                {
                    // Ensure that we can assign from source to parameter type
                    if (elementType.Equals(typeof(string))
                        || elementType.Equals(typeof(byte[]))
                        || elementType.Equals(typeof(ReadOnlyMemory<byte>))
                        || elementType.Equals(typeof(long))
                        || elementType.Equals(typeof(double)))
                    {
                        target = context.Source switch
                        {
                            IEnumerable<string> source => source.ToArray(),
                            IEnumerable<ReadOnlyMemory<byte>> source => GetBinaryData(source, elementType!),
                            IEnumerable<double> source => source.ToArray(),
                            IEnumerable<long> source => source.ToArray(),
                            _ => null
                        };
                    }
                }
            }

            return target is not null;
        }

        private static object? GetBinaryData(IEnumerable<ReadOnlyMemory<byte>> source, Type targetType)
        {
            if (targetType.IsAssignableFrom(typeof(ReadOnlyMemory<byte>)))
            {
                return source.ToArray();
            }
            else
            {
                return source.Select(i => i.ToArray()).ToArray();
            }
        }
    }
}
