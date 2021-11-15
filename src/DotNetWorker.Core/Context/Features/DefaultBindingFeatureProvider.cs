// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.Functions.Worker.Converters;

namespace Microsoft.Azure.Functions.Worker.Context.Features
{
    internal class DefaultBindingFeatureProvider : IInvocationFeatureProvider
    {
        private readonly IConverterContextFactory _converterContextFactory;
        private static readonly Type _featureType = typeof(IModelBindingFeature);

        public DefaultBindingFeatureProvider(IConverterContextFactory converterContextFactory)
        {
            _converterContextFactory = converterContextFactory ?? throw new ArgumentNullException(nameof(converterContextFactory));
        }
        
        public bool TryCreate(Type type, out object? feature)
        {
            feature = type == _featureType
                ? new DefaultModelBindingFeature(_converterContextFactory)
                : null;

            return feature is not null;
        }
    }
}
