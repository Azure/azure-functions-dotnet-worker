// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    /// <summary>
    /// An abstraction to get IInputConverter instances.
    /// </summary>
    internal interface IInputConverterProvider
    {
        /// <summary>
        /// Gets a collection of registered input converter instances in the order they were registered.
        /// This includes the default converters and the ones explicitly registered by user.
        /// </summary>
        IEnumerable<IInputConverter> RegisteredInputConverters { get; }

        /// <summary>
        /// Gets an instance of the converter for the type requested.
        /// </summary>
        /// <param name="converterTypeName">The assembly qualified name of the type for which we are requesting an IInputConverter instance.</param>
        /// <returns>IInputConverter instance of the requested type.</returns>
        IInputConverter GetOrCreateConverterInstance(string converterTypeName);
    }
}
