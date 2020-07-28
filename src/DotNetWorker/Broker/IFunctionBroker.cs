﻿using System.Collections.Generic;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;

namespace Microsoft.Azure.Functions.DotNetWorker
{
    public interface IFunctionBroker
    {
        void AddFunction(FunctionLoadRequest functionLoadRequest);
        object Invoke(InvocationRequest invocationRequest, out List<ParameterBinding> parameterBindings);
    }
}
