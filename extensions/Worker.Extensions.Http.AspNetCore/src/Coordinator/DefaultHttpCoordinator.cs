// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.Infrastructure;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore
{
    internal class DefaultHttpCoordinator : IHttpCoordinator
    {
        private const int HttpContextTimeoutInSeconds = 5;
        private const int FunctionContextTimeoutInSeconds = 5;
        private const int FunctionStartTimeoutInSeconds = 15;

        private readonly ConcurrentDictionary<string, ContextReference> _contextReferenceList;
        private readonly ExtensionTrace _logger;

        public DefaultHttpCoordinator(ExtensionTrace extensionTrace)
        {
            _contextReferenceList = new ConcurrentDictionary<string, ContextReference>();
            _logger = extensionTrace;
        }

        public async Task<FunctionContext> SetHttpContextAsync(string invocationId, HttpContext context)
        {
            var contextRef = _contextReferenceList.GetOrAdd(invocationId, static id => new ContextReference(id));
            if (!contextRef.HttpContextValueSource.TrySetResult(context))
            {
                if (contextRef.HttpContextValueSource.Task.IsCanceled)
                {
                    throw new OperationCanceledException($"HTTP context for invocation id '{invocationId}' was cancelled.");
                }
                
                if (contextRef.FunctionContextValueSource.Task.IsCompleted)
                {
                    return await contextRef.FunctionContextValueSource.Task;
                }
                
                throw new InvalidOperationException($"Failed to set HTTP context for invocation id '{invocationId}'.");
            }

            _logger.HttpContextSet(invocationId, context.TraceIdentifier);

            try
            {
                return await contextRef.FunctionContextValueSource.Task.WaitAsync(TimeSpan.FromSeconds(FunctionContextTimeoutInSeconds), context.RequestAborted);
            }
            catch (OperationCanceledException e)
            {
                throw new OperationCanceledException($"HTTP request was cancelled while waiting for the function context to be set. Invocation: '{invocationId}'.", e);
            }
            catch (TimeoutException e)
            {
                throw new TimeoutException($"Timed out waiting for the HTTP context to be set. Invocation: '{invocationId}'.", e);
            }
        }

        public async Task<HttpContext> SetFunctionContextAsync(string invocationId, FunctionContext context)
        {
            var contextRef = _contextReferenceList.GetOrAdd(invocationId, static id => new ContextReference(id));

            if (!contextRef.FunctionContextValueSource.TrySetResult(context))
            {
                if (contextRef.FunctionContextValueSource.Task.IsCanceled)
                {
                    throw new OperationCanceledException($"Function context for invocation id '{invocationId}' was cancelled.");
                }

                if(contextRef.HttpContextValueSource.Task.IsCompleted)
                {
                    return await contextRef.HttpContextValueSource.Task;
                }

                throw new InvalidOperationException($"Failed to set function context for invocation id '{invocationId}'.");
            }

            _logger.FunctionContextSet(invocationId);

            int waitStep = 0;
            try
            {
                // block here until it's time to start the function
                _ = await contextRef.FunctionStartTask.Task.WaitAsync(TimeSpan.FromSeconds(FunctionStartTimeoutInSeconds), context.CancellationToken);

                waitStep = 1;
                return await contextRef.HttpContextValueSource.Task.WaitAsync(TimeSpan.FromSeconds(HttpContextTimeoutInSeconds), context.CancellationToken);
            }
            catch (OperationCanceledException e)
            {
                string message = waitStep == 0
                    ? $"Function invocation cancelled while waiting for start call. Invocation: '{invocationId}'."
                    : $"Function invocation cancelled while waiting for HTTP context. Invocation: '{invocationId}'.";

                throw new OperationCanceledException(message, e);
            }
            catch (TimeoutException e)
            {
                string message = waitStep == 0
                    ? $"Timed out waiting for the function start call. Invocation: '{invocationId}'."
                    : $"Timed out waiting for the HTTP context to be set. Invocation: '{invocationId}'.";

                throw new TimeoutException(message, e);
            }
        }

        public Task RunFunctionInvocationAsync(string invocationId)
        {
            if (!_contextReferenceList.TryGetValue(invocationId, out var contextReference))
            {
                throw new InvalidOperationException($"Context for invocation id '{invocationId}' does not exist.");
            }

            return contextReference.InvokeFunctionAsync();
        }

        // TODO:See about making this not public
        public void CompleteFunctionInvocation(string invocationId)
        {
            // This is the last step; remove the context reference
            if (_contextReferenceList.TryRemove(invocationId, out var contextRef))
            {
                contextRef.CompleteFunction();
            }
            else
            {
                // do something here?
            }
        }
    }

}
