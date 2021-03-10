// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Diagnostics;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Azure.Functions.Worker
{
    internal class WorkerHostedService : IHostedService
    {
        private readonly IWorker _worker;
        private readonly IWorkerDiagnostics _diagnostics;

        public WorkerHostedService(IWorker worker, IWorkerDiagnostics diagnostics)
        {
            _worker = worker ?? throw new ArgumentNullException(nameof(worker));
            _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _worker.StartAsync(cancellationToken);

            _diagnostics.OnApplicationCreated(WorkerInformation.Instance);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return _worker.StopAsync(cancellationToken);
        }
    }
}
