// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Azure.Functions.Worker
{
    internal class DefaultInvocationFeaturesFactory : IInvocationFeaturesFactory
    {
        private readonly IEnumerable<IInvocationFeatureProvider> _providers;

        public DefaultInvocationFeaturesFactory(IEnumerable<IInvocationFeatureProvider> providers)
        {
            _providers = providers ?? throw new System.ArgumentNullException(nameof(providers));
        }

        public IInvocationFeatures Create()
        {
            return new InvocationFeatures(_providers);
        }
    }
}
