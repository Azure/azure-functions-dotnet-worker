﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Azure.Functions.Worker.Core.Http
{
    internal class HttpContextReference
    {
        private TaskCompletionSource<FunctionContext> _functionCompletionTask = new TaskCompletionSource<FunctionContext>();
        private TaskCompletionSource<HttpContext> _httpContextValueSource = new TaskCompletionSource<HttpContext>();
        private string _invocationId;
        private CancellationToken _token;

        public HttpContextReference(string invocationId)
        {
            _invocationId = invocationId;
        }

        public HttpContextReference(string invocationId, HttpContext context)
        {
            _invocationId = invocationId;
            _httpContextValueSource.SetResult(context);
        }

        public TaskCompletionSource<FunctionContext> FunctionCompletionTask { get => _functionCompletionTask; set => _functionCompletionTask = value; }

        public TaskCompletionSource<HttpContext> HttpContextValueSource { get => _httpContextValueSource; set => _httpContextValueSource = value; }

        internal void SetCancellationToken(CancellationToken token)
        {
            _token = token;
        }

        internal void CompleteFunction(FunctionContext functionContext)
        {
            if (_httpContextValueSource.Task.IsCompleted)
            {
                if (_httpContextValueSource.Task.IsCanceled || _token.IsCancellationRequested)
                {
                    _functionCompletionTask.SetCanceled();
                }
                else
                {
                    _functionCompletionTask.SetResult(functionContext);
                }
            }
            else
            {
                // we should never reach here b/c the class that calls this needs httpContextValueSource to complete to reach this method
            }
        }
    }
}
