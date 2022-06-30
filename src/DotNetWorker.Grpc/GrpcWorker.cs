// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Azure.Core.Serialization;
using Grpc.Core;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Azure.Functions.Worker.Grpc;
using Microsoft.Azure.Functions.Worker.Grpc.Features;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Invocation;
using Microsoft.Azure.Functions.Worker.OutputBindings;
using Microsoft.Azure.Functions.Worker.Rpc;
using Microsoft.Extensions.Options;
using static Microsoft.Azure.Functions.Worker.Grpc.Messages.FunctionRpc;


namespace Microsoft.Azure.Functions.Worker
{
    internal class GrpcWorker : IWorker
    {
        private readonly IGrpcWorkerMessageFactory _factory;
        private readonly ChannelReader<StreamingMessage> _outputReader;
        private readonly ChannelWriter<StreamingMessage> _outputWriter;
        private readonly IOptions<GrpcWorkerStartupOptions> _startupOptions;
        private readonly FunctionRpcClient _rpcClient;

        public GrpcWorker(
            GrpcHostChannel outputChannel,
            IGrpcWorkerMessageFactory factory,
            IOptions<GrpcWorkerStartupOptions> startupOptions,
            FunctionRpcClient rpcClient)
        {
            if (outputChannel == null)
            {
                throw new ArgumentNullException(nameof(outputChannel));
            }

            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _startupOptions = startupOptions ?? throw new ArgumentNullException(nameof(startupOptions));
            _rpcClient = rpcClient ?? throw new ArgumentNullException(nameof(rpcClient));

            _outputReader = outputChannel.Channel.Reader;
            _outputWriter = outputChannel.Channel.Writer;
        }

        public async Task StartAsync(CancellationToken token)
        {
            var eventStream = _rpcClient.EventStream(cancellationToken: token);

            await SendStartStreamMessageAsync(eventStream.RequestStream);

            _ = StartWriterAsync(eventStream.RequestStream);
            _ = StartReaderAsync(eventStream.ResponseStream);
        }

        public Task StopAsync(CancellationToken token)
        {
            return Task.CompletedTask;
        }

        private async Task SendStartStreamMessageAsync(IClientStreamWriter<StreamingMessage> requestStream)
        {
            StartStream str = new StartStream()
            {
                WorkerId = _startupOptions.Value.WorkerId
            };

            StreamingMessage startStream = new StreamingMessage()
            {
                StartStream = str
            };

            await requestStream.WriteAsync(startStream);
        }

        private async Task StartWriterAsync(IClientStreamWriter<StreamingMessage> requestStream)
        {
            await foreach (StreamingMessage rpcWriteMsg in _outputReader.ReadAllAsync())
            {
                await requestStream.WriteAsync(rpcWriteMsg);
            }
        }

        private async Task StartReaderAsync(IAsyncStreamReader<StreamingMessage> responseStream)
        {
            while (await responseStream.MoveNext())
            {
                await ProcessRequestAsync(responseStream.Current);
            }
        }

        private Task ProcessRequestAsync(StreamingMessage request)
        {
            // Dispatch and return.
            Task.Run(() => ProcessRequestCoreAsync(request));
            return Task.CompletedTask;
        }

        private async Task ProcessRequestCoreAsync(StreamingMessage request)
        {
            var handler = _factory.CreateHandler(request.ContentCase);
            if (handler == null)
            {
                // TODO: Trace failure here.
                return;
            }

            await _outputWriter.WriteAsync(await handler.HandleMessageAsync(request));
        }

        internal static async Task<InvocationResponse> InvocationRequestHandlerAsync(
            InvocationRequest request,
            IFunctionsApplication application,
            IInvocationFeaturesFactory invocationFeaturesFactory,
            ObjectSerializer serializer, 
            IOutputBindingsInfoProvider outputBindingsInfoProvider,
            IInputConversionFeatureProvider functionInputConversionFeatureProvider)
        {
            FunctionContext? context = null;
            InvocationResponse response = new()
            {
                InvocationId = request.InvocationId
            };

            try
            {
                var invocation = new GrpcFunctionInvocation(request);

                IInvocationFeatures invocationFeatures = invocationFeaturesFactory.Create();
                invocationFeatures.Set<FunctionInvocation>(invocation);
                invocationFeatures.Set<IExecutionRetryFeature>(invocation);

                context = application.CreateContext(invocationFeatures);
                invocationFeatures.Set<IFunctionBindingsFeature>(new GrpcFunctionBindingsFeature(context, request, outputBindingsInfoProvider));

                if (functionInputConversionFeatureProvider.TryCreate(typeof(DefaultInputConversionFeature), out var conversion))
                {                                    
                    invocationFeatures.Set<IInputConversionFeature>(conversion!);
                }

                await application.InvokeFunctionAsync(context);

                var functionBindings = context.GetBindings();

                foreach (var binding in functionBindings.OutputBindingData)
                {
                    var parameterBinding = new ParameterBinding
                    {
                        Name = binding.Key
                    };

                    if (binding.Value is not null)
                    {
                        parameterBinding.Data = await binding.Value.ToRpcAsync(serializer);
                    }

                    response.OutputData.Add(parameterBinding);
                }

                if (functionBindings.InvocationResult != null)
                {
                    TypedData? returnVal = await functionBindings.InvocationResult.ToRpcAsync(serializer);

                    response.ReturnValue = returnVal;
                }

                response.Result = new StatusResult
                {
                    Status = StatusResult.Types.Status.Success
                };
            }
            catch (Exception ex)
            {
                response.Result = new StatusResult
                {
                    Exception = ex.ToRpcException(),
                    Status = StatusResult.Types.Status.Failure
                };
            }
            finally
            {
                (context as IDisposable)?.Dispose();
            }

            return response;
        }

        internal static FunctionLoadResponse FunctionLoadRequestHandler(
            FunctionLoadRequest request,
            IFunctionsApplication application,
            IMethodInfoLocator methodInfoLocator)
        {
            var response = new FunctionLoadResponse
            {
                FunctionId = request.FunctionId,
                Result = StatusResult.Success
            };

            if (!request.Metadata.IsProxy)
            {
                try
                {
                    FunctionDefinition definition = request.ToFunctionDefinition(methodInfoLocator);
                    application.LoadFunction(definition);
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

            return response;
        }
    }
}
