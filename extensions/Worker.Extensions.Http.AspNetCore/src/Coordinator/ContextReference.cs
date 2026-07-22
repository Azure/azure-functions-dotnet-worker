// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore
{
    internal class ContextReference
    {
        private readonly TaskCompletionSource<bool> _functionStartTask = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> _functionCompletionTask = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private CancellationTokenRegistration _cancellationRegistration;

        private TaskCompletionSource<HttpContext> _httpContextValueSource = new();
        private TaskCompletionSource<FunctionContext> _functionContextValueSource = new();

        public ContextReference(string invocationId)
        {
            InvocationId = invocationId;
        }

        public string InvocationId { get; private set; }

        public TaskCompletionSource<bool> FunctionStartTask { get => _functionStartTask; }

        public TaskCompletionSource<HttpContext> HttpContextValueSource { get => _httpContextValueSource; set => _httpContextValueSource = value; }

        public TaskCompletionSource<FunctionContext> FunctionContextValueSource { get => _functionContextValueSource; set => _functionContextValueSource = value; }

        internal Task InvokeFunctionAsync(CancellationToken cancellationToken)
        {
            _functionStartTask.SetResult(true);

            if (cancellationToken.CanBeCanceled)
            {
                _cancellationRegistration = cancellationToken.Register(
                    static state =>
                    {
                        var (tcs, ct) = ((TaskCompletionSource<bool>, CancellationToken))state!;
                        tcs.TrySetCanceled(ct);
                    },
                    (_functionCompletionTask, cancellationToken));
            }

            return _functionCompletionTask.Task;
        }

        internal void CompleteFunction()
        {
            _cancellationRegistration.Dispose();

            if (_functionCompletionTask.Task.IsCompleted)
            {
                return;
            }

            if (_httpContextValueSource.Task.IsCompleted)
            {
                _functionCompletionTask.TrySetResult(true);
            }
            else
            {
                // we should never reach here b/c the class that calls this needs httpContextValueSource to complete to reach this method
            }
        }
    }
}
