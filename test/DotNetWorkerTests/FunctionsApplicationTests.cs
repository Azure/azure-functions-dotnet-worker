// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class FunctionsApplicationTests
    {
        private readonly IFunctionsApplication _application;
        private readonly Mock<FunctionExecutionDelegate> _mockFunctionExecutionDelegate = new Mock<FunctionExecutionDelegate>();
        private readonly Mock<IServiceScopeFactory> _mockServiceScopeFactory = new Mock<IServiceScopeFactory>();

        public FunctionsApplicationTests()
        {
            _mockFunctionExecutionDelegate.Setup(p => p(It.IsAny<FunctionContext>())).Returns(Task.CompletedTask);

            var options = new WorkerOptions();
            var wrapper = new OptionsWrapper<WorkerOptions>(options);
            var contextFactory = new DefaultFunctionContextFactory(_mockServiceScopeFactory.Object);

            _application = new FunctionsApplication(_mockFunctionExecutionDelegate.Object, contextFactory, wrapper, NullLogger<FunctionsApplication>.Instance);
        }

        [Fact]
        public async void Context_Disposed()
        {
            var context = new TestFunctionContext();
            _application.LoadFunction(context.FunctionDefinition);

            var result = await _application.InvokeFunctionAsync(context);

            Assert.Equal(StatusResult.Types.Status.Success, result.Result.Status);
            Assert.True(context.IsDisposed);
        }
    }
}
