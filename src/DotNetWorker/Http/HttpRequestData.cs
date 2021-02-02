// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.Azure.Functions.Worker
{
    public abstract class HttpRequestData
    {
        public abstract IImmutableDictionary<string, string> Headers { get; }

        public abstract string Body { get; }

        public abstract IImmutableDictionary<string, string> Params { get; }

        public abstract IImmutableDictionary<string, string> Query { get; }
    }
}
