// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    /// <summary>
    /// An attribute that can specify a type of <see cref="Type"/> to use for function input conversion.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Parameter |
        AttributeTargets.Class |
        AttributeTargets.Interface |
        AttributeTargets.Enum |
        AttributeTargets.Struct)]
    public sealed class SupportedConverterTypesAttribute : Attribute
    {
        /// <summary>
        /// Gets the input converter type.
        /// </summary>
        public List<Type>? Types { get; }

        /// <summary>
        /// Creates a new instance of <see cref="SupportedConverterTypesAttribute"/>
        /// </summary>
        /// <param name="types">The input converter type.</param>
        /// <exception cref="InvalidOperationException">Thrown when the converterType parameter value is a type which has not implemented Microsoft.Azure.Functions.Worker.Converters.IInputConverter</exception>
        public SupportedConverterTypesAttribute(params Type[] types)
        {
            if (types == null || types.Length == 0)
            {
                throw new ArgumentNullException(nameof(types));
            }

            Types = new List<Type> { };

            foreach (var type in types)
            {
                Types.Add(type);
            }
        }
    }
}
