using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace FunctionsDotNetWorker
{
    internal class MyHostedService : IHostedService
    {
        private readonly FunctionRpcClient _rpcClient;

        public MyHostedService(FunctionRpcClient rpcClient)
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
