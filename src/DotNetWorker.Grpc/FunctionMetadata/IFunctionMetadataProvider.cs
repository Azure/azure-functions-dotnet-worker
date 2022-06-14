// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker
{
    internal interface IFunctionMetadataProvider
    {

        /// <summary>
        /// Gets all function metadata that this provider knows about asynchronously
        /// </summary>
        /// <returns>A Task with IEnumerable of FunctionMetadata</returns>
        Task<ImmutableArray<RpcFunctionMetadata>> GetFunctionMetadataAsync(string directory);
    }
}
