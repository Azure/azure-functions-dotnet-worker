using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FunctionsDotNetWorker.Logging;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;
using Microsoft.Extensions.Logging;

namespace FunctionsDotNetWorker
{
    public class FunctionExecutionContext
    {
        public FunctionExecutionContext(InvocationRequest invocationRequest, string funcName, WorkerLogManager workerLogManager)
        {
            InvocationId = invocationRequest.InvocationId;
            FunctionName = funcName;
            TraceContext = invocationRequest.TraceContext;
            Logger = workerLogManager.GetInvocationLogger(InvocationId); 
        }

        public FunctionExecutionContext(string invocationId, string funcName, RpcTraceContext traceContext)
        {
            FunctionName = funcName;
            InvocationId = invocationId;
            TraceContext = traceContext;
            //Logger = FunctionRpcClient.getInvocationLogger(invocationId); maybe add something in functionRPC client to get the invocation logger?
        }
        public string FunctionName { get; private set; }
        public string InvocationId { get; private set; }
        public InvocationLogger Logger { get; private set; }
        public RpcTraceContext TraceContext { get; private set; }
    }
}
