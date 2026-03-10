// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker.Diagnostics;
using OpenTelemetry;

namespace Microsoft.Azure.Functions.Worker.OpenTelemetry
{
    internal class DefaultBaggagePropagator : IBaggagePropagator
    {
        public IDisposable? SetBaggage(IEnumerable<KeyValuePair<string, string>> baggage)
        {
            foreach (var kv in baggage)
            {
                Baggage.SetBaggage(kv.Key, kv.Value);
            }

            return new BaggageScope();      
        }

        private sealed class BaggageScope : IDisposable
        {
            public void Dispose()
            {
                Baggage.ClearBaggage();
            }
        }
    }
}
