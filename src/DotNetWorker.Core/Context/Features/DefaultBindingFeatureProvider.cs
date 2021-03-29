// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker.Converters;

namespace Microsoft.Azure.Functions.Worker.Context.Features
{
    internal class DefaultBindingFeatureProvider : IInvocationFeatureProvider
    {
        private static readonly Type _featureType = typeof(IModelBindingFeature);
        private readonly IEnumerable<IConverter> _converters;

        public DefaultBindingFeatureProvider(IEnumerable<IConverter> converters)
        {
            _converters = converters ?? throw new ArgumentNullException(nameof(converters));
        }

        public bool TryCreate(Type type, out object? feature)
        {
            feature = type == _featureType
                ? new DefaultModelBindingFeature(_converters)
                : null;

            return feature is not null;
        }
    }
}
