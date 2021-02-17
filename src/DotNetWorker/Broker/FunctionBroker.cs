// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Context;
using Microsoft.Azure.Functions.Worker.Pipeline;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;
using Status = Microsoft.Azure.WebJobs.Script.Grpc.Messages.StatusResult.Types.Status;

namespace Microsoft.Azure.Functions.Worker
{
    internal class FunctionBroker : IFunctionBroker
    {
        private readonly ConcurrentDictionary<string, FunctionDefinition> _functionMap = new ConcurrentDictionary<string, FunctionDefinition>();
        private readonly FunctionExecutionDelegate _functionExecutionDelegate;
        private readonly IFunctionContextFactory _functionContextFactory;
        private readonly IFunctionDefinitionFactory _functionDescriptorFactory;

        public FunctionBroker(FunctionExecutionDelegate functionExecutionDelegate, IFunctionContextFactory functionContextFactory, IFunctionDefinitionFactory functionDescriptorFactory)
        {
            _functionExecutionDelegate = functionExecutionDelegate ?? throw new ArgumentNullException(nameof(functionExecutionDelegate));
            _functionContextFactory = functionContextFactory ?? throw new ArgumentNullException(nameof(functionContextFactory));
            _functionDescriptorFactory = functionDescriptorFactory ?? throw new ArgumentNullException(nameof(functionDescriptorFactory));
        }

        public void AddFunction(FunctionLoadRequest functionLoadRequest)
        {
            FunctionDefinition functionDefinition = _functionDescriptorFactory.Create(functionLoadRequest);

            if (functionDefinition.Metadata.FunctionId is null)
            {
                throw new InvalidOperationException("The function ID for the current load request is invalid");
            }

            _functionMap.TryAdd(functionDefinition.Metadata.FunctionId, functionDefinition);
        }


        public async Task<InvocationResponse> InvokeAsync(FunctionInvocation invocation)
        {
            // TODO: File InvocationResponse removal issue
            InvocationResponse response = new InvocationResponse
            {
                InvocationId = invocation.InvocationId
            };

            FunctionContext? executionContext = null;

            try
            {
                executionContext = _functionContextFactory.Create(invocation, _functionMap[invocation.FunctionId]);

                await _functionExecutionDelegate(executionContext);

                var parameterBindings = executionContext.OutputBindings;
                var result = executionContext.InvocationResult;

                foreach (var paramBinding in parameterBindings)
                {
                    // TODO: ParameterBinding shouldn't happen here

                    foreach (var binding in executionContext.OutputBindings)
                    {
                        var parameterBinding = new ParameterBinding
                        {
                            Name = binding.Key,
                            Data = binding.Value.ToRpc()
                        };
                        response.OutputData.Add(parameterBinding);
                    }
                }
                if (result != null)
                {
                    var returnVal = result.ToRpc();

                    response.ReturnValue = returnVal;
                }

                response.Result = new StatusResult { Status = Status.Success };
            }
            catch (Exception ex)
            {
                response.Result = new StatusResult
                {
                    Exception = ex.ToRpcException(),
                    Status = Status.Failure
                };
            }
            finally
            {
                (executionContext as IDisposable)?.Dispose();
            }

            return response;
        }
    }

}
