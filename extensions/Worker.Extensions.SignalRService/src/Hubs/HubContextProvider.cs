// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;

namespace Microsoft.Azure.Functions.Worker.SignalRService
{
    internal class HubContextProvider
    {
        private static readonly ConcurrentDictionary<Type, object> Cache = new();

        public bool TryGetValue(Type type, out object value) => Cache.TryGetValue(type, out value);

        public void Add(Type type, object value) => Cache.TryAdd(type, value);
    }
}
