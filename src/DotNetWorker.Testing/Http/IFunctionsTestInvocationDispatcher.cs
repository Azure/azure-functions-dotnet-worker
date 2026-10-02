// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Testing;

internal sealed record FunctionsTestHttpRequest(
    string Method,
    Uri Url,
    IReadOnlyDictionary<string, string> Headers,
    ReadOnlyMemory<byte> Body);

internal interface IFunctionsTestInvocationDispatcher
{
    string ApplicationDirectory { get; }

    Task<FunctionInvocationResult> InvokeHttpAsync(
        string functionName,
        FunctionsTestHttpRequest request,
        string invocationId,
        CancellationToken cancellationToken);
}
