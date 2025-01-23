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
            contextRef.HttpContextValueSource.SetResult(context);

            _logger.HttpContextSet(invocationId, context.TraceIdentifier);

            try
            {
                return await contextRef.FunctionContextValueSource.Task.WaitAsync(TimeSpan.FromSeconds(FunctionContextTimeoutInSeconds));
            }
            catch (TimeoutException e)
            {
                throw new TimeoutException($"Timed out waiting for the HTTP context to be set. Invocation: '{invocationId}'.", e);
            }
        }

        public async Task<HttpContext> SetFunctionContextAsync(string invocationId, FunctionContext context)
        {
            var contextRef = _contextReferenceList.GetOrAdd(invocationId, static id => new ContextReference(id));
            contextRef.FunctionContextValueSource.SetResult(context);

            _logger.FunctionContextSet(invocationId);

            int waitStep = 0;
            try
            {
                // block here until it's time to start the function
                _ = await contextRef.FunctionStartTask.Task.WaitAsync(TimeSpan.FromSeconds(FunctionStartTimeoutInSeconds));

                waitStep = 1;
                return await contextRef.HttpContextValueSource.Task.WaitAsync(TimeSpan.FromSeconds(HttpContextTimeoutInSeconds));
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
