// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker.Converters;

namespace Microsoft.Azure.Functions.Worker.Context.Features
{
    /// <summary>
    /// An abstraction to get IInputConverter instances.
    /// </summary>
    public interface IInputConverterProvider
    {
        /// <summary>
        /// Gets an ordered collection of default converter instances.
        /// </summary>
        IEnumerable<IInputConverter> DefaultConverters { get; }
        
        /// <summary>
        /// Gets an instance of the converter for the type requested.
        /// </summary>
        /// <param name="converterType">The type of IInputConverter implementation to return.</param>
        /// <returns>IInputConverter instance of the requested type.</returns>
        IInputConverter GetOrCreateConverterInstance(Type converterType);
    }
}
