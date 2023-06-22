// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Grpc.Core;

namespace Microsoft.Azure.Functions.Worker.Extensions.Rpc;

/// <summary>
/// Contains options for the functions gRPC invocation.
/// </summary>
public sealed class FunctionsGrpcOptions
{
    /// <summary>
    /// Gets the <see cref="CallInvoker" /> which is configured to call the functions host.
    /// </summary>
    public CallInvoker CallInvoker { get; internal set; } = null!;
}
