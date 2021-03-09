// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Extension methods for getting features from <see cref="IInvocationFeatures" />.
    /// </summary>
    internal static class InvocationsFeaturesExtensions
    {
        /// <summary>
        /// Gets a feature of type <typeparamref name="T"/> from the <see cref="IInvocationFeatures"/>.
        /// </summary>
        /// <typeparam name="T">The type of the feature to get.</typeparam>
        /// <param name="features">The <see cref="IInvocationFeatures"/>.</param>
        /// <returns>A feature object of type <typeparamref name="T"/>.</returns>
        /// <exception cref="InvalidOperationException">There is no feature of type <typeparamref name="T"/>.</exception>
        public static T GetRequired<T>(this IInvocationFeatures features)
        {
            var feature = features.Get<T>();

            if (feature is null)
            {
                throw new InvalidOperationException($"No feature is registered with the type {typeof(T)}.");
            }

            return feature;
        }

        /// <summary>
        /// Tries to get a feature of type <typeparamref name="T"/> from the <see cref="IInvocationFeatures"/>.
        /// </summary>
        /// <typeparam name="T">The type of the feature to get.</typeparam>
        /// <param name="features">The <see cref="IInvocationFeatures"/>.</param>
        /// <param name="feature">The feature, if found. Otherwise, null.</param>
        /// <returns>True if the feature was found. Otherwise, false.</returns>        
        public static bool TryGet<T>(this IInvocationFeatures features, out T? feature)
        {
            feature = features.Get<T>();
            return feature is not null;
        }
    }
}
