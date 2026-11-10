// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Grpc;

internal interface IExtensionPayloadProvider
{
    /// <summary>
    /// Opens a complete payload at its beginning. The caller owns disposal; seeking and length are not required.
    /// Preparation failures leave no resources for the caller to clean up.
    /// </summary>
    Task<Stream> OpenReadAsync(CancellationToken cancellationToken);
}
