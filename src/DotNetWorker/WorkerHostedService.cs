using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Azure.Functions.DotNetWorker
{
    internal class WorkerHostedService : IHostedService
    {
        private readonly FunctionRpcClient _rpcClient;

        public WorkerHostedService(FunctionRpcClient rpcClient)
        {
            _rpcClient = rpcClient;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var startAndWriter = _rpcClient.RpcStream();
            var readerTask = _rpcClient.RpcStreamReader();

            return Task.WhenAll(startAndWriter, readerTask);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
