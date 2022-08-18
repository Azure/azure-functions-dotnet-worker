// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;

namespace Microsoft.Azure.Functions.Worker.Invocation
{
    internal class FunctionInvocationManager : IFunctionInvocationManager
    {
        internal ConcurrentDictionary<string, FunctionInvocationDetails> _inflightInvocations;

        public FunctionInvocationManager()
        {
          _inflightInvocations = new ConcurrentDictionary<string, FunctionInvocationDetails>();
        }

        public void TryAddInvocationDetails(string invocationId, FunctionInvocationDetails details)
        {
            if (string.IsNullOrEmpty(invocationId) || details is null)
            {
              return;
            }

            _inflightInvocations.TryAdd(invocationId, details);
        }

        public void TryRemoveInvocationDetails(string invocationId)
        {
            if (string.IsNullOrEmpty(invocationId))
            {
              return;
            }

            _inflightInvocations.TryRemove(invocationId, out FunctionInvocationDetails? details);
        }

        public FunctionInvocationDetails? TryGetInvocationDetails(string invocationId)
        {
            if (string.IsNullOrEmpty(invocationId))
            {
              return null;
            }

            _inflightInvocations.TryGetValue(invocationId, out FunctionInvocationDetails? details);
            return details;
        }
    }
}
