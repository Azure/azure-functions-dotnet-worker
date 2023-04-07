// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    /// <summary>
    /// An attribute that can specify a type of <see cref="IInputConverter"/> to use for function input conversion.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Parameter |
        AttributeTargets.Class |
        AttributeTargets.Interface |
        AttributeTargets.Enum |
        AttributeTargets.Struct)]
    public sealed class InputConverterAttribute : Attribute
    {
        /// <summary>
        /// Gets the value of disable converter fallback flag
        /// </summary>
        public bool DisableConverterFallback { get; }

        /// <summary>
        /// Gets the input converter type.
        /// </summary>
        public List<Type> ConverterTypes { get; }

        /// <summary>
        /// Creates a new instance of <see cref="InputConverterAttribute"/>
        /// </summary>
        /// <param name="disableConverterFallback">disable converter fallback flag.</param>
        /// <param name="converterTypes">The input converter type.</param>
        /// <exception cref="InvalidOperationException">Thrown when the converterType parameter value is a type which has not implemented Microsoft.Azure.Functions.Worker.Converters.IInputConverter</exception>
        public InputConverterAttribute(bool disableConverterFallback = false, params Type[] converterTypes)
        {
            DisableConverterFallback = disableConverterFallback;

            if (converterTypes == null || converterTypes.Length == 0)
            {
                throw new ArgumentNullException(nameof(converterTypes));
            }

            ConverterTypes = new List<Type> { };

            foreach (var converterType in converterTypes)
            {
                var interfaceType = typeof(IInputConverter);
                if (!interfaceType.IsAssignableFrom(converterType))
                {
                    throw new InvalidOperationException($"{converterType.Name} must implement {interfaceType.FullName} to be used as an input converter.");
                }

                ConverterTypes.Add(converterType);
            }
        }
    }
}
