using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;

namespace Microsoft.Azure.Functions.DotNetWorker
{
    internal interface IFunctionsHostClient
    {
        Task ProcessRequestAsync(StreamingMessage request);
    }
}
