﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.DotNetWorker.FunctionInvoker
{
    interface IFunctionInvoker
    {
        Task InvokeAsync(FunctionExecutionContext context);
    }
}
