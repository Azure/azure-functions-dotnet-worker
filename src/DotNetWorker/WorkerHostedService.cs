// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Azure.Functions.Worker
{
    internal class WorkerHostedService : IHostedService
    {
        private readonly IWorker _worker;

        public WorkerHostedService(IWorker worker)
        {
            _worker = worker ?? throw new ArgumentNullException(nameof(worker));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return _worker.StartAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return _worker.StopAsync(cancellationToken);
        }
    }
}
