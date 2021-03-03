// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    internal class EnumerableConverter : IConverter
    {
        // Converting IEnumerable<> to common types
        // Currently, only to array but written to be extensible
        public bool TryConvert(ConverterContext context, out object? target)
        {
            EnumerableTargetType? targetType = null;
            // Array
            if (context.Parameter.Type.IsArray)
            {
                targetType = EnumerableTargetType.Array;
            }

            // Only apply if user is requesting an array, list, or hashset
            if (targetType is not null)
            {
                // Valid options from FunctionRpc.proto are string, byte, double and long collection
                if (context.Source is IEnumerable<string> enumerableString)
                {
                    target = GetTarget(enumerableString, targetType);
                    return true;
                }
                else if (context.Source is IEnumerable<byte[]> enumerableBytes)
                {
                    target = GetTarget(enumerableBytes, targetType);
                    return true;
                }
                else if (context.Source is IEnumerable<double> enumerableDouble)
                {
                    target = GetTarget(enumerableDouble, targetType);
                    return true;
                }
                else if (context.Source is IEnumerable<long> enumerableLong)
                {
                    target = GetTarget(enumerableLong, targetType);
                    return true;
                }
            }

            target = default;
            return false;
        }

        // Dictionary and Lookup not handled because we don't know 
        // what they keySelector and elementSelector should be.
        private static object? GetTarget<T>(IEnumerable<T> source, EnumerableTargetType? targetType)
        {
            switch (targetType)
            {
                case EnumerableTargetType.Array:
                    return source.ToArray();
                default:
                    return null;
            }
        }

        private enum EnumerableTargetType
        {
            Array,
            List,
            Dictionary,
            HashSet,
            Lookup
        }
    }
}
