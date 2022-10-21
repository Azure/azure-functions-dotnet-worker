// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Functions.Worker.Extensions.Abstractions
{
    /// <summary>
    /// Provides information about which input converters to be used.
    /// </summary>
    /// <remarks>
    /// Input bindings/trigger binding shall implement this interface to provide information
    /// about which specific converters to be used when input conversion is performed for that binding.
    /// </remarks>
    public interface IInputConverterProvider
    {
        /// <summary>
        /// Gets an ordered collection of <see cref="System.Type"/> instances representing the converters to be used.
        /// Each entry in the collection should be the <see cref="System.Type"/> of a class which implements
        /// the Microsoft.Azure.Functions.Worker.Converters.IInputConverter interface.
        /// </summary>
        public IList<Type> ConverterTypes { get; }
    }
}
