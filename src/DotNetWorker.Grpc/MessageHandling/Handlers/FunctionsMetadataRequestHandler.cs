// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Rpc;

namespace Microsoft.Azure.Functions.Worker.Grpc;

internal class FunctionsMetadataRequestHandler : IGrpcWorkerMessageHandler
{
    private readonly IFunctionMetadataProvider _functionMetadataProvider;

    public FunctionsMetadataRequestHandler(IFunctionMetadataProvider functionMetadataProvider)
    {
        _functionMetadataProvider = functionMetadataProvider;
    }

    public async Task<StreamingMessage> HandleMessageAsync(StreamingMessage request)
    {
        var responseMessage = new StreamingMessage
        {
            RequestId = request.RequestId,
        };

        var response = new FunctionMetadataResponse
        {
            Result = StatusResult.Success,
            UseDefaultMetadataIndexing = false
        };

        try
        {
            var functionMetadataList = await _functionMetadataProvider.GetFunctionMetadataAsync(
                request.FunctionsMetadataRequest.FunctionAppDirectory);

            foreach (var func in functionMetadataList)
            {
                response.FunctionMetadataResults.Add(func);
            }
        }
        catch (Exception ex)
        {
            response.Result = new StatusResult
            {
                Status = StatusResult.Types.Status.Failure,
                Exception = ex.ToRpcException()
            };
        }

        responseMessage.FunctionMetadataResponse = response;
        return responseMessage;
    }
}
