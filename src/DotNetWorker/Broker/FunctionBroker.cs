// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Context;
using Microsoft.Azure.Functions.Worker.Diagnostics;
using Microsoft.Azure.Functions.Worker.Pipeline;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Status = Microsoft.Azure.WebJobs.Script.Grpc.Messages.StatusResult.Types.Status;

namespace Microsoft.Azure.Functions.Worker
{
    internal partial class FunctionBroker : IFunctionBroker
    {
        private readonly ConcurrentDictionary<string, FunctionDefinition> _functionMap = new ConcurrentDictionary<string, FunctionDefinition>();
        private readonly FunctionExecutionDelegate _functionExecutionDelegate;
        private readonly IFunctionContextFactory _functionContextFactory;
        private readonly IFunctionDefinitionFactory _functionDefinitionFactory;
        private readonly ILogger<FunctionBroker> _logger;
        private readonly IOptions<WorkerOptions> _workerOptions;

        public FunctionBroker(FunctionExecutionDelegate functionExecutionDelegate, IFunctionContextFactory functionContextFactory,
            IFunctionDefinitionFactory functionDefinitionFactory, IOptions<WorkerOptions> workerOptions, ILogger<FunctionBroker> logger)
        {
            _functionExecutionDelegate = functionExecutionDelegate ?? throw new ArgumentNullException(nameof(functionExecutionDelegate));
            _functionContextFactory = functionContextFactory ?? throw new ArgumentNullException(nameof(functionContextFactory));
            _workerOptions = workerOptions ?? throw new ArgumentNullException(nameof(workerOptions));
            _functionDefinitionFactory = functionDefinitionFactory ?? throw new ArgumentNullException(nameof(functionDefinitionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void AddFunction(FunctionLoadRequest functionLoadRequest)
        {
            FunctionDefinition functionDefinition = _functionDefinitionFactory.Create(functionLoadRequest);

            if (functionDefinition.Metadata.FunctionId is null)
            {
                throw new InvalidOperationException("The function ID for the current load request is invalid");
            }

            Log.FunctionDefinitionCreated(_logger, functionDefinition);
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
            var functionDefinition = _functionMap[invocation.FunctionId];

            var scope = new FunctionInvocationScope(functionDefinition.Metadata.Name, invocation.InvocationId);
            using (_logger.BeginScope(scope))
            {
                try
                {
                    executionContext = _functionContextFactory.Create(invocation, functionDefinition);

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
                                Data = binding.Value.ToRpc(_workerOptions.Value.Serializer)
                            };
                            response.OutputData.Add(parameterBinding);
                        }
                    }
                    if (result != null)
                    {
                        TypedData? returnVal = await result.ToRpcAsync(_workerOptions.Value.Serializer);

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
            }

            return response;
        }
    }
}
