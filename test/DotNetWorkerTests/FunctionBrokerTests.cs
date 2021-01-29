using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Context;
using Microsoft.Azure.Functions.Worker.Pipeline;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;
using Moq;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class FunctionBrokerTests
    {
        private readonly FunctionBroker _functionBroker;
        private readonly Mock<FunctionExecutionDelegate> _mockFunctionExecutionDelegate = new Mock<FunctionExecutionDelegate>();
        private readonly Mock<IFunctionExecutionContextFactory> _mockFunctionExecutionContextFactory = new Mock<IFunctionExecutionContextFactory>();
        private readonly Mock<IFunctionDefinitionFactory> _mockFunctionDefinitionFactory = new Mock<IFunctionDefinitionFactory>();

        public FunctionBrokerTests()
        {
            _functionBroker = new FunctionBroker(_mockFunctionExecutionDelegate.Object, _mockFunctionExecutionContextFactory.Object, _mockFunctionDefinitionFactory.Object);
        }

        [Fact]
        public async void DiposeExecutionContextTestAsync()
        {
            var invocationRequest = new TestFunctionInvocation();
            invocationRequest.FunctionId = "123";

            var functionDescriptor = new FunctionMetadata();
            functionDescriptor.FunctionId = "123";

            var definition = new TestFunctionDefinition
            {
                Metadata = functionDescriptor
            };

            var context = new TestFunctionExecutionContext();
            _mockFunctionDefinitionFactory.Setup(p => p.Create(It.IsAny<FunctionLoadRequest>())).Returns(definition);
            _mockFunctionExecutionContextFactory.Setup(p => p.Create(It.IsAny<FunctionInvocation>(), definition)).Returns(context);
            _mockFunctionExecutionDelegate.Setup(p => p(It.IsAny<FunctionExecutionContext>())).Returns(Task.CompletedTask);

            _functionBroker.AddFunction(It.IsAny<FunctionLoadRequest>());
            var result = await _functionBroker.InvokeAsync(invocationRequest);
            Assert.Equal(StatusResult.Types.Status.Success, result.Result.Status);
            Assert.True(context.IsDisposed);
        }
    }
}
