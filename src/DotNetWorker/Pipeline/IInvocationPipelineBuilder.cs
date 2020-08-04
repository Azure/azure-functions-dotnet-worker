using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.DotNetWorker.Pipeline
{
    public interface IInvocationPipelineBuilder<FunctionExecutionContext>
    {
        IInvocationPipelineBuilder<FunctionExecutionContext> Use(Func<FunctionExecutionDelegate, FunctionExecutionDelegate> middleware);

        FunctionExecutionDelegate Build();
    }
}
