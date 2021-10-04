// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Context.Features
{
    /// <summary>
    /// Provider abstraction to get IInputConversionFeature instance.
    /// </summary>
    internal interface IInputConversionFeatureProvider
    {
        /// <summary>
        /// Tries to create an instance of IInputConversionFeature feature.
        /// </summary>
        /// <param name="type">The feature type.</param>
        /// <param name="feature">The IInputConversionFeature instance created or null.</param>
        /// <returns>True if the creation was successful, else False.</returns>
        bool TryCreate(Type type, out IInputConversionFeature? feature);
    }
}
