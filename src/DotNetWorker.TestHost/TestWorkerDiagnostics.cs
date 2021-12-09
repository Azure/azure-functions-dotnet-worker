using Microsoft.Azure.Functions.Worker.Diagnostics;

namespace Microsoft.Azure.Functions.Worker.TestHost
{
    internal class TestWorkerDiagnostics : IWorkerDiagnostics
    {
        public void OnApplicationCreated(WorkerInformation workerInfo)
        {
        }

        public void OnFunctionLoaded(FunctionDefinition definition)
        {
        }
    }
}
