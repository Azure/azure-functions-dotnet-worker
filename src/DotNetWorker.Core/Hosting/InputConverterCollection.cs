// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker.Converters;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// A collection of input converters.
    /// </summary>
    public sealed class InputConverterCollection : IEnumerable<Type>
    {
        // Passing initial capacity as a tiny optimization since we know we will be registering
        // at-least 7 built-in converters to this collection shortly while bootstrapping.
        private readonly IList<Type> _converterTypes = new List<Type>(capacity: 7);

        /// <summary>
        /// Registers an input converter type.
        /// </summary>
        /// <typeparam name="T">The input converter type. This type must implement <see cref="IInputConverter"/></typeparam>
        public void Register<T>() where T : IInputConverter
        {
            _converterTypes.Add(typeof(T));
        }

        /// <summary>
        /// Registers an input converter type at the specific index.
        /// </summary>
        /// <typeparam name="T">The input converter type. This type must implement <see cref="IInputConverter"/></typeparam>
        public void RegisterAt<T>(int index) where T : IInputConverter
        {
            _converterTypes.Insert(index, typeof(T));
        }

        /// <summary>
        /// Removes all items from the collection.
        /// </summary>
        public void Clear()
        {
            _converterTypes.Clear();
        }

        /// <inheritdoc />
        public IEnumerator<Type> GetEnumerator() => _converterTypes.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
