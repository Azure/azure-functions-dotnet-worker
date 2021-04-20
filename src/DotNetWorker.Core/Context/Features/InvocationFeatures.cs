// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Azure.Functions.Worker
{
    internal class InvocationFeatures : IInvocationFeatures
    {
        private readonly Dictionary<Type, object> _features = new();
        private readonly IEnumerable<IInvocationFeatureProvider> _featureProviders;

        public InvocationFeatures(IEnumerable<IInvocationFeatureProvider> featureProviders)
        {
            _featureProviders = featureProviders;
        }

        public T? Get<T>()
        {
            var type = typeof(T);
            if (!_features.TryGetValue(type, out object? feature))
            {
                if (_featureProviders.Any(t => t.TryCreate(type, out feature)) && !_features.TryAdd(type, feature!))
                {
                    feature = _features[type];
                }
            }

            return feature is null ? default : (T)feature;
        }

        public IEnumerator<KeyValuePair<Type, object>> GetEnumerator()
        {
            return _features.GetEnumerator();
        }

        public void Set<T>(T instance)
        {
            if (instance is null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            _features[typeof(T)] = instance;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _features.GetEnumerator();
        }
    }
}
