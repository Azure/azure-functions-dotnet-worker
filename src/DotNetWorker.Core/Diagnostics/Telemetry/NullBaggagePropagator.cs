// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker.Diagnostics;

namespace Microsoft.Azure.Functions.Worker.Core.Diagnostics.Telemetry
{
    internal sealed class NullBaggagePropagator : IBaggagePropagator
    {
        public static readonly NullBaggagePropagator Instance = new();

        private NullBaggagePropagator()
        {
        }

        public IDisposable? SetBaggage(IEnumerable<KeyValuePair<string, string>> baggage)
        {
            // No-op. This propagator is used when baggage propagation is explicitly disabled.
            return null;
        }
    }
}
