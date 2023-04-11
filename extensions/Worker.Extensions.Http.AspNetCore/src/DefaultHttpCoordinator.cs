// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Azure.Functions.Worker.Core.Http
{
    internal class DefaultHttpCoordinator : IHttpCoordinator
    {
        private ConcurrentDictionary<string, HttpContextReference> _httpContextReferenceList;

        public DefaultHttpCoordinator()
        {
            _httpContextReferenceList = new ConcurrentDictionary<string, HttpContextReference>();
        }

        public Task<FunctionContext> SetContextAsync(string invocationId, HttpContext context)
        {
            var httpContextRef = _httpContextReferenceList.AddOrUpdate(invocationId,
                s => new HttpContextReference(invocationId, context), (s, c) =>
                {
                    c.HttpContextValueSource.SetResult(context);
                    return c;
                });

            return httpContextRef.FunctionCompletionTask.Task;
        }

        public Task<HttpContext> GetContextAsync(string invocationId, CancellationToken cancellationToken)
        {
            var httpContextRef = _httpContextReferenceList.GetOrAdd(invocationId, new HttpContextReference(invocationId));
            httpContextRef.SetCancellationToken(cancellationToken);

            return httpContextRef.HttpContextValueSource.Task;
        }

        // TODO:See about making this not public
        public void CompleteInvocation(string functionId, FunctionContext functionContext)
        {
            if (_httpContextReferenceList.TryGetValue(functionId, out var httpContextRef))
            {
                httpContextRef.CompleteFunction(functionContext);
            }
            else
            {
                // do something here?
            }
        }
    }

}
