using Microsoft.Azure.WebJobs.Script.Grpc.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.DotNetWorker.Pipeline
{
    public interface IFunctionExecutionContextFactory
    {
        FunctionExecutionContext Create(InvocationRequest request);
    }
}
