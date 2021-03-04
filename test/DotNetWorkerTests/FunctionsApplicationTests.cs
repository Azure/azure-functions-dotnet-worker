// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Moq;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class FunctionsApplicationTests
    {
        private readonly Mock<IFunctionsApplication> _mockApplication = new Mock<IFunctionsApplication>(MockBehavior.Strict);

        public FunctionsApplicationTests()
        {
            _mockApplication
                .Setup(m => m.InvokeFunctionAsync(It.IsAny<FunctionContext>()))
                .Returns(Task.CompletedTask);
        }

        [Fact]
        public async Task Context_Disposed()
        {
            var context = new TestFunctionContext();

            var response = await GrpcWorker.InvokeAsync(_mockApplication.Object, new JsonObjectSerializer(), context);

            Assert.Equal(StatusResult.Types.Status.Success, response.Result.Status);
            Assert.True(context.IsDisposed);
        }
    }
}
