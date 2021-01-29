using Microsoft.Azure.Functions.Worker.Context;

namespace Microsoft.Azure.Functions.Worker.Pipeline
{
    internal interface IFunctionExecutionContextFactory
    {
        FunctionExecutionContext Create(FunctionInvocation invocation, FunctionDefinition definition);
    }
}
