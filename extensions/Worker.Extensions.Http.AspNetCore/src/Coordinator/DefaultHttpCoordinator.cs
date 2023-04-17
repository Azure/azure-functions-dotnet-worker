// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore
{
    internal class DefaultHttpCoordinator : IHttpCoordinator
    {
        private readonly ConcurrentDictionary<string, ContextReference> _contextReferenceList;

        public DefaultHttpCoordinator()
        {
            _contextReferenceList = new ConcurrentDictionary<string, ContextReference>();
        }

        public Task<FunctionContext> SetHttpContextAsync(string invocationId, HttpContext context)
        {
            var contextRef = _contextReferenceList.GetOrAdd(invocationId, static id => new ContextReference(id));
            contextRef.HttpContextValueSource.SetResult(context);

            return contextRef.FunctionContextValueSource.Task;
        }

        public async Task<HttpContext> SetFunctionContextAsync(string invocationId, FunctionContext context)
        {
            var contextRef = _contextReferenceList.GetOrAdd(invocationId, static id => new ContextReference(id));
            contextRef.SetCancellationToken(context.CancellationToken);
            contextRef.FunctionContextValueSource.SetResult(context);

            // block here until it's time to start the function
            await contextRef.FunctionStartTask.Task;
            return await contextRef.HttpContextValueSource.Task;
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
                contextRef.Dispose();
            }
            else
            {
                // do something here?
            }
        }
    }

}
