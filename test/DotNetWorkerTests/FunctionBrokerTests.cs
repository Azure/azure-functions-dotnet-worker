using Moq;
using Xunit;
using Microsoft.Azure.Functions.DotNetWorker;
using Microsoft.Azure.Functions.DotNetWorker.Pipeline;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.DotNetWorkerTests
{
    public class FunctionBrokerTests
    {
        FunctionBroker _functionBroker;
        Mock<FunctionExecutionDelegate> _mockFunctionExecutionDelegate = new Mock<FunctionExecutionDelegate>();
        Mock<IFunctionExecutionContextFactory> _mockFunctionExecutionContextFactory = new Mock<IFunctionExecutionContextFactory>();

        public FunctionBrokerTests()
        {
            _functionBroker = new FunctionBroker(_mockFunctionExecutionDelegate.Object, _mockFunctionExecutionContextFactory.Object);
        }

        [Fact]
        public async void DisposeContextSuccessfullyTest()
        {
            InvocationRequest invocationRequest = new InvocationRequest();
            _mockFunctionExecutionDelegate.Setup(p => p(It.IsAny<FunctionExecutionContext>())).Returns(Task.CompletedTask);
            var result = await _functionBroker.InvokeAsync(invocationRequest);
        }

    }
}
