// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Context;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker
{
    internal interface IFunctionBroker
    {
        // TODO: Interface made internal as it should be refactored
        // to remove a dependency on proto/gRPC generated types
        void AddFunction(FunctionLoadRequest functionLoadRequest);

        Task<InvocationResponse> InvokeAsync(FunctionInvocation invocation);
    }
}
