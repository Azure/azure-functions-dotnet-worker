// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Pipeline;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class FunctionBrokerTests
    {
        private readonly FunctionBroker _functionBroker;
        private readonly Mock<FunctionExecutionDelegate> _mockFunctionExecutionDelegate = new Mock<FunctionExecutionDelegate>();
        private readonly Mock<IFunctionContextFactory> _mockFunctionContextFactory = new Mock<IFunctionContextFactory>();
        private readonly Mock<IFunctionDefinitionFactory> _mockFunctionDefinitionFactory = new Mock<IFunctionDefinitionFactory>();

        public FunctionBrokerTests()
        {
            var options = new WorkerOptions();
            var wrapper = new OptionsWrapper<WorkerOptions>(options);
            _functionBroker = new FunctionBroker(_mockFunctionExecutionDelegate.Object, _mockFunctionContextFactory.Object,
                _mockFunctionDefinitionFactory.Object, wrapper, NullLogger<FunctionBroker>.Instance);
        }

        [Fact]
        public async void DiposeExecutionContextTestAsync()
        {
            var invocation = new TestFunctionInvocation(functionId: "123");
            var definition = new TestFunctionDefinition(functionId: "123");

            var context = new TestFunctionContext(definition, invocation);
            _mockFunctionDefinitionFactory.Setup(p => p.Create(It.IsAny<FunctionLoadRequest>())).Returns(definition);
            _mockFunctionContextFactory.Setup(p => p.Create(It.IsAny<FunctionInvocation>(), definition)).Returns(context);
            _mockFunctionExecutionDelegate.Setup(p => p(It.IsAny<FunctionContext>())).Returns(Task.CompletedTask);

            _functionBroker.AddFunction(It.IsAny<FunctionLoadRequest>());
            var result = await _functionBroker.InvokeAsync(invocation);
            Assert.Equal(StatusResult.Types.Status.Success, result.Result.Status);
            Assert.True(context.IsDisposed);
        }
    }
}
