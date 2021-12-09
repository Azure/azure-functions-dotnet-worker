using Microsoft.Azure.Functions.Worker.Invocation;
using Microsoft.Azure.WebJobs.Script.Description;

namespace Microsoft.Azure.Functions.Worker.TestHost
{
    internal class TestFunctionDefinitionFactory
    {
        private readonly IMethodInfoLocator _methodInfoLocator;

        public TestFunctionDefinitionFactory(IMethodInfoLocator methodInfoLocator)
        {
            _methodInfoLocator = methodInfoLocator;
        }

        public FunctionDefinition Create(FunctionMetadata functionMetadata) => new TestFunctionDefinition(functionMetadata, _methodInfoLocator);
    }
}
