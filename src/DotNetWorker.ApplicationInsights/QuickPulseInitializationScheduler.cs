// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Logging.ApplicationInsights
{
    // We need to be able to delay initialization in Functions for cold start, 
    // so perform this initialization in a service that can handle cancellation.
    internal class QuickPulseInitializationScheduler : IDisposable
    {
        private readonly CancellationTokenSource _cts = new();
        private readonly object _lock = new();
        private bool _disposed = false;

        public void ScheduleInitialization(Action initialization, TimeSpan? delay)
        {
            Task.Delay(delay.GetValueOrDefault(TimeSpan.Zero), _cts.Token)
                .ContinueWith(_ =>
                {
                    lock (_lock)
                    {
                        if (!_disposed)
                        {
                            initialization();
                        }
                    }
                },
                TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (!_disposed)
                {
                    _cts.Cancel();
                    _cts.Dispose();

                    _disposed = true;
                }
            }
        }
    }
}
