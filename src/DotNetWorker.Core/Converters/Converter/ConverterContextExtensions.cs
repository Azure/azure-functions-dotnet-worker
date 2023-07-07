// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    /// <summary>
    /// Provides extension methods to work with an <see cref="ConverterContext"/> instance.
    /// </summary>
    public static class ConverterContextExtensions
    {
        /// <summary>
        /// Tries to retrieve the binding attribute from the <see cref="ConverterContext"/>.
        /// </summary>
        /// <param name="context">The converter context.</param>
        /// <param name="bindingAttribute">When this method returns, contains the binding attribute if found; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if the binding attribute is found in the converter context; otherwise, <c>false</c>.</returns>
        public static bool TryGetBindingAttribute<T>(this ConverterContext context, out object? bindingAttribute) where T : Attribute
            => context.Properties.TryGetValue(PropertyBagKeys.BindingAttribute, out bindingAttribute);

        /// <summary>
        /// Tries to retrieve the binding attribute from the <see cref="ConverterContext"/>.
        /// </summary>
        /// <param name="context">The converter context.</param>
        /// <param name="type">Binding attribute type.</param>
        /// <param name="bindingAttribute">When this method returns, contains the binding attribute if found; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if the binding attribute is found in the converter context; otherwise, <c>false</c>.</returns>
        public static bool TryGetBindingAttribute(this ConverterContext context, Type type, out object? bindingAttribute)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (!typeof(Attribute).IsAssignableFrom(type))
            {
                throw new ArgumentException($"The type '{type}' must be an attribute.", nameof(type));
            }

            return context.Properties.TryGetValue(PropertyBagKeys.BindingAttribute, out bindingAttribute);
        }
    }
}
