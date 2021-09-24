// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Context.Features
{
    internal class DefaultBindingFeatureProvider : IInvocationFeatureProvider
    {
        private static readonly Type _featureType = typeof(IModelBindingFeature);

        public bool TryCreate(Type type, out object? feature)
        {
            feature = type == _featureType
                ? new DefaultModelBindingFeature()
                : null;

            return feature is not null;
        }
    }
}
