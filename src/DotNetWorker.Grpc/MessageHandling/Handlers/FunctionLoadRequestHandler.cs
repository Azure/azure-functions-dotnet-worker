// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Invocation;
using Microsoft.Azure.Functions.Worker.Rpc;

namespace Microsoft.Azure.Functions.Worker.Grpc;

internal class FunctionLoadRequestHandler : IGrpcWorkerMessageHandler
{
    private readonly IMethodInfoLocator _methodInfoLocator;
    private readonly IFunctionsApplication _application;

    public FunctionLoadRequestHandler(IMethodInfoLocator methodInfoLocator, IFunctionsApplication application)
    {
        _methodInfoLocator = methodInfoLocator;
        _application = application;
    }

    public Task<StreamingMessage> HandleMessageAsync(StreamingMessage request)
    {
        var responseMessage = new StreamingMessage
        {
            RequestId = request.RequestId,
        };

        var response = new FunctionLoadResponse
        {
            FunctionId = request.FunctionLoadRequest.FunctionId,
            Result = StatusResult.Success
        };

        if (!request.FunctionLoadRequest.Metadata.IsProxy)
        {
            try
            {
                var definition = request.FunctionLoadRequest.ToFunctionDefinition(_methodInfoLocator);
                _application.LoadFunction(definition);
            }
            catch (Exception ex)
            {
                response.Result = new StatusResult
                {
                    Status = StatusResult.Types.Status.Failure,
                    Exception = ex.ToRpcException()
                };
            }
        }

        return Task.FromResult(responseMessage);
    }
}
