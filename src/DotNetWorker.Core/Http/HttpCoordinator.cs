// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Azure.Functions.Worker.Core.Http
{
    internal class HttpCoordinator : IHttpCoordinator
    {
        private ConcurrentDictionary<string, HttpContextReference> _httpContextReferenceList;

        public HttpCoordinator()
        {
            _httpContextReferenceList = new ConcurrentDictionary<string, HttpContextReference>();
        }


        public Task SetContextAsync(string invocationId, HttpContext context)
        {
            var httpContext = _httpContextReferenceList.AddOrUpdate(invocationId,
                s=> new HttpContextReference(invocationId, context), (s, c) =>
                {
                    c.HttpContextValueSource.SetResult(context);
                    return c;
                });

            return httpContext.FunctionCompletionTask.Task;
        }

        public Task<HttpContext> GetContextAsync(string invocationId)
        {
            var httpContext = _httpContextReferenceList.GetOrAdd(invocationId, new HttpContextReference(invocationId));

            return httpContext.HttpContextValueSource.Task;
        }

        // TODO:See about making this not public
        public void CompleteInvocation(string functionId)
        {
            _httpContextReferenceList.TryGetValue(functionId, out var httpContextRef);

            if (httpContextRef != null)
            {
                httpContextRef.CompleteFunction();
            }
            else
            {
                // do something here?
            }
        }
    }

}
