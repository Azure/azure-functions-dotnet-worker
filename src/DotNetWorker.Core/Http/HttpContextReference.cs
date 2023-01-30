using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Azure.Functions.Worker.Core.Http
{
    internal class HttpContextReference
    {
        private TaskCompletionSource<HttpContext> _functionCompletionTask = new TaskCompletionSource<HttpContext>();
        private TaskCompletionSource<HttpContext> _httpContextValueSource = new TaskCompletionSource<HttpContext>();
        private string _invocationId;
        private HttpContext? _httpContext;

        public HttpContextReference(string invocationId)
        {
            _invocationId = invocationId;
        }

        public HttpContextReference(string invocationId, HttpContext context)
        {
            _invocationId = invocationId;
            _httpContext = context;
            _httpContextValueSource.SetResult(context);
        }

        public TaskCompletionSource<HttpContext> FunctionCompletionTask { get => _functionCompletionTask; set => _functionCompletionTask = value; }

        public TaskCompletionSource<HttpContext> HttpContextValueSource { get => _httpContextValueSource; set => _httpContextValueSource = value; }

        internal void CompleteFunction()
        {
            if (_httpContext is not null)
            {
                FunctionCompletionTask.SetResult(_httpContext);
            }
            else
            {
                // throw some error?
                // what does it mean if the function completes without httpContext set / is it possible?
            }
        }
    }
}
