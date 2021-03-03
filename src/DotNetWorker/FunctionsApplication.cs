// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Diagnostics;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Pipeline;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Status = Microsoft.Azure.Functions.Worker.Grpc.Messages.StatusResult.Types.Status;

namespace Microsoft.Azure.Functions.Worker
{
    internal partial class FunctionsApplication : IFunctionsApplication
    {
        private readonly ConcurrentDictionary<string, FunctionDefinition> _functionMap = new ConcurrentDictionary<string, FunctionDefinition>();
        private readonly FunctionExecutionDelegate _functionExecutionDelegate;
        private readonly IFunctionContextFactory _functionContextFactory;
        private readonly ILogger<FunctionsApplication> _logger;
        private readonly IOptions<WorkerOptions> _workerOptions;

        public FunctionsApplication(FunctionExecutionDelegate functionExecutionDelegate, IFunctionContextFactory functionContextFactory,
             IOptions<WorkerOptions> workerOptions, ILogger<FunctionsApplication> logger)
        {
            _functionExecutionDelegate = functionExecutionDelegate ?? throw new ArgumentNullException(nameof(functionExecutionDelegate));
            _functionContextFactory = functionContextFactory ?? throw new ArgumentNullException(nameof(functionContextFactory));
            _workerOptions = workerOptions ?? throw new ArgumentNullException(nameof(workerOptions));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public FunctionContext CreateContext(IInvocationFeatures features)
        {
            var invocation = features.Get<FunctionInvocation>() ?? throw new InvalidOperationException($"The {nameof(FunctionInvocation)} feature is required.");

            var functionDefinition = _functionMap[invocation.FunctionId];
            features.Set<FunctionDefinition>(functionDefinition);

            return _functionContextFactory.Create(features);
        }

        public Task<WorkerInitResponse> InitializeWorkerAsync(WorkerInitRequest request)
        {
            var response = new WorkerInitResponse()
            {
                Result = new StatusResult { Status = Status.Success }
            };

            response.Capabilities.Add("RpcHttpBodyOnly", bool.TrueString);
            response.Capabilities.Add("RawHttpBodyBytes", bool.TrueString);
            response.Capabilities.Add("RpcHttpTriggerMetadataRemoved", bool.TrueString);
            response.Capabilities.Add("UseNullableValueDictionaryForHttp", bool.TrueString);

            response.WorkerVersion = typeof(FunctionsApplication).Assembly.GetName().Version?.ToString();

            return Task.FromResult(response);
        }

        public void LoadFunction(FunctionDefinition definition)
        {
            if (definition.Id is null)
            {
                throw new InvalidOperationException("The function ID for the current load request is invalid");
            }

            Log.FunctionDefinitionCreated(_logger, definition);
            _functionMap.TryAdd(definition.Id, definition);
        }

        public async Task<InvocationResponse> InvokeFunctionAsync(FunctionContext context)
        {
            // TODO: File InvocationResponse removal issue
            InvocationResponse response = new InvocationResponse()
            {
                InvocationId = context.Invocation.Id
            };

            var scope = new FunctionInvocationScope(context.FunctionDefinition.Name, context.Invocation.Id);
            using (_logger.BeginScope(scope))
            {
                try
                {
                    await _functionExecutionDelegate(context);

                    var parameterBindings = context.OutputBindings;
                    var result = context.InvocationResult;

                    // TODO: ParameterBinding shouldn't happen here
                    foreach (var binding in parameterBindings)
                    {
                        var parameterBinding = new ParameterBinding
                        {
                            Name = binding.Key,
                            Data = await binding.Value.ToRpcAsync(_workerOptions.Value.Serializer)
                        };
                        response.OutputData.Add(parameterBinding);
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
                    (context as IDisposable)?.Dispose();
                }
            }

            return response;
        }

        public Task<FunctionEnvironmentReloadResponse> ReloadEnvironmentAsync(FunctionEnvironmentReloadRequest request)
        {
            var response = new FunctionEnvironmentReloadResponse
            {
                Result = new StatusResult { Status = Status.Success }
            };

            return Task.FromResult(response);
        }
    }
}
