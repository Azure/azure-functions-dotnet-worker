using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker
{
    internal interface IFunctionsHostClient
    {
        Task ProcessRequestAsync(StreamingMessage request);
    }
}
