// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;

namespace Microsoft.Azure.Functions.Worker.Invocation
{
    internal class FunctionInvocationDictionary : IFunctionInvocationDictionary
    {
        private ConcurrentDictionary<string, FunctionInvocationDetails> _inflightInvocations;

        public FunctionInvocationDictionary()
        {
            _inflightInvocations = new ConcurrentDictionary<string, FunctionInvocationDetails>();
        }

        public bool TryAddInvocationDetails(string invocationId, FunctionInvocationDetails details)
        {
            if (string.IsNullOrEmpty(invocationId))
            {
                throw new ArgumentNullException(nameof(invocationId));
            }

            return _inflightInvocations.TryAdd(invocationId, details);
        }

        public bool TryRemoveInvocationDetails(string invocationId)
        {
            if (string.IsNullOrEmpty(invocationId))
            {
                throw new ArgumentNullException(nameof(invocationId));
            }

            return _inflightInvocations.TryRemove(invocationId, out var details);
        }

        public bool TryGetInvocationDetails(string invocationId, out FunctionInvocationDetails details)
        {
            if (string.IsNullOrEmpty(invocationId))
            {
                throw new ArgumentNullException(nameof(invocationId));
            }

            return _inflightInvocations.TryGetValue(invocationId, out details);
        }
    }
}
