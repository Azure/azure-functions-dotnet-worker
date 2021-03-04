// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    // Converting IEnumerable<> to Array
    internal class EnumerableConverter : IConverter
    {
        private static Type ListType = typeof(List<>);
        private static Type HashSetType = typeof(HashSet<>);

        // Convert IEnumerable from common types. IEnumerable types will
        // be converted by TypeConverter.
        public bool TryConvert(ConverterContext context, out object? target)
        {
            EnumerableTargetType? targetType = null;
            target = null;
            // Array
            if (context.Parameter.Type.IsArray)
            {
                targetType = EnumerableTargetType.Array;
            }
            // List or HashSet
            else if (context.Parameter.Type.IsGenericType)
            {
                if (context.Parameter.Type.GetGenericTypeDefinition().IsAssignableFrom(ListType))
                {
                    targetType = EnumerableTargetType.List;
                }
                else if (context.Parameter.Type.GetGenericTypeDefinition().IsAssignableFrom(HashSetType))
                {
                    targetType = EnumerableTargetType.HashSet;
                }
            }

            // Only apply if user is requesting an array, list, or hashset
            if (targetType is not null)
            {           
                // Valid options from FunctionRpc.proto are string, byte, double and long collection
                target = context.Source switch
                {
                    IEnumerable<string> source => GetTarget(source, targetType),
                    IEnumerable<byte[]> source => GetTarget(source, targetType),
                    IEnumerable<double> source => GetTarget(source, targetType),
                    IEnumerable<long> source => GetTarget(source, targetType),
                    _ => null
                };
            }

            if (target is null)
            {
                target = default;
                return false;
            }
            else
            {
                return true;
            }
        }

        // Dictionary and Lookup not handled because we don't know 
        // what they keySelector and elementSelector should be.
        private static object? GetTarget<T>(IEnumerable<T> source, EnumerableTargetType? targetType)
        {
            return targetType switch
            {
                EnumerableTargetType.Array => source.ToArray(),
                EnumerableTargetType.HashSet => source.ToHashSet(),
                EnumerableTargetType.List => source.ToList(),
                _ => null,
            };
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
