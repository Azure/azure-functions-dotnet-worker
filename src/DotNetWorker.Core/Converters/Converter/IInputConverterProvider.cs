// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    /// <summary>
    /// An abstraction to get IInputConverter instances.
    /// </summary>
    internal interface IInputConverterProvider
    {
        /// <summary>
        /// Gets an ordered collection of converter instances.
        /// This includes the default converters and the ones explicitly registered by user.
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
