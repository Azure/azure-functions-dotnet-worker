// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore
{
    internal class ContextReference : IDisposable
    {
        private readonly TaskCompletionSource<bool> _functionStartTask = new();
        private readonly TaskCompletionSource<bool> _functionCompletionTask = new();

        private TaskCompletionSource<HttpContext> _httpContextValueSource = new();
        private TaskCompletionSource<FunctionContext> _functionContextValueSource = new();

        private CancellationToken _token;
        private CancellationTokenRegistration _tokenRegistration;

        public ContextReference(string invocationId)
        {
            InvocationId = invocationId;
        }

        public string InvocationId { get; private set; }

        public TaskCompletionSource<bool> FunctionStartTask { get => _functionStartTask; }

        public TaskCompletionSource<HttpContext> HttpContextValueSource { get => _httpContextValueSource; set => _httpContextValueSource = value; }

        public TaskCompletionSource<FunctionContext> FunctionContextValueSource { get => _functionContextValueSource; set => _functionContextValueSource = value; }

        internal void SetCancellationToken(CancellationToken token)
        {
            _token = token;
            _tokenRegistration = _token.Register(() =>
            {
                _functionStartTask.TrySetCanceled();
                _functionCompletionTask.TrySetCanceled();
                _functionContextValueSource.TrySetCanceled();
                _httpContextValueSource.TrySetCanceled();
            });
        }

        internal Task InvokeFunctionAsync()
        {
            _functionStartTask.SetResult(true);
            return _functionCompletionTask.Task;
        }

        internal void CompleteFunction()
        {
            if (_functionCompletionTask.Task.IsCompleted)
            {
                return;
            }

            if (_httpContextValueSource.Task.IsCompleted)
            {
                if (_httpContextValueSource.Task.IsCanceled || _token.IsCancellationRequested)
                {
                    _functionCompletionTask.TrySetCanceled();
                }
                else
                {
                    _functionCompletionTask.TrySetResult(true);
                }
            }
            else
            {
                // we should never reach here b/c the class that calls this needs httpContextValueSource to complete to reach this method
            }
        }

        public void Dispose()
        {
            if (_tokenRegistration != default)
            {
                _tokenRegistration.Dispose();
            }
        }
    }
}
