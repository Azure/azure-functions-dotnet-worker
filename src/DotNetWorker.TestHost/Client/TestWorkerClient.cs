using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.TestHost
{
    public abstract class TestWorkerClient
    {
        public abstract Task<InvocationResult> InvokeAsync(string functionName, InvocationContext invocationContext);

        public abstract InvocationContext CreateContext();
    }
}
