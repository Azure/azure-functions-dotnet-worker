// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Azure.Functions.Worker.Diagnostics
{
    internal class FunctionInvocationScope : IReadOnlyList<KeyValuePair<string, object>>
    {
        internal const string FunctionInvocationIdKey = "AzureFunctions_InvocationId";
        internal const string FunctionNameKey = "AzureFunctions_FunctionName";

        private readonly string _invocationId;
        private readonly string _functionName;

        private string? _cachedToString;

        public FunctionInvocationScope(string functionName, string invocationid)
        {
            _functionName = functionName;
            _invocationId = invocationid;
        }

        public KeyValuePair<string, object> this[int index]
        {
            get
            {
                return index switch
                {
                    0 => new KeyValuePair<string, object>(FunctionInvocationIdKey, _invocationId),
                    1 => new KeyValuePair<string, object>(FunctionNameKey, _functionName),
                    _ => throw new ArgumentOutOfRangeException(nameof(index)),
                };
            }
        }

        public int Count => 2;

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            for (var i = 0; i < Count; ++i)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override string ToString()
        {
            if (_cachedToString == null)
            {
                _cachedToString = FormattableString.Invariant($"{FunctionNameKey}:{_functionName} {FunctionInvocationIdKey}:{_invocationId}");
            }

            return _cachedToString;
        }
    }
}
