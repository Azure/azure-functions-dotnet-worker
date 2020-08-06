using Microsoft.Azure.Functions.DotNetWorker;
using Microsoft.Azure.Functions.DotNetWorker.Descriptor;
using Microsoft.Azure.Functions.DotNetWorker.Logging;
using Microsoft.Azure.Functions.DotNetWorker.Pipeline;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;
using Moq;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Azure.Functions.DotNetWorkerTests
{
    public class FunctionBrokerTests
    {
        FunctionBroker _functionBroker;
        Mock<FunctionExecutionDelegate> _mockFunctionExecutionDelegate = new Mock<FunctionExecutionDelegate>();
        Mock<IFunctionExecutionContextFactory> _mockFunctionExecutionContextFactory = new Mock<IFunctionExecutionContextFactory>();
        Mock<IFunctionDescriptorFactory> _mockFunctionDescriptorFactory = new Mock<IFunctionDescriptorFactory>();

        public FunctionBrokerTests()
        {
            _functionBroker = new FunctionBroker(_mockFunctionExecutionDelegate.Object, _mockFunctionExecutionContextFactory.Object, _mockFunctionDescriptorFactory.Object);
        }

        [Fact]
        public async void DiposeExecutionContextTestAsync()
        {
            InvocationRequest invocationRequest = new InvocationRequest();
            invocationRequest.FunctionId = "123";
            FunctionDescriptor functionDescriptor = new FunctionDescriptor();
            functionDescriptor.FunctionId = "123";
            TestFunctionExecutionContext context = new TestFunctionExecutionContext();
            _mockFunctionDescriptorFactory.Setup(p => p.Create(It.IsAny<FunctionLoadRequest>())).Returns(functionDescriptor);
            _mockFunctionExecutionContextFactory.Setup(p => p.Create(It.IsAny<InvocationRequest>())).Returns(context);
            _mockFunctionExecutionDelegate.Setup(p => p(It.IsAny<FunctionExecutionContext>())).Returns(Task.CompletedTask);

            _functionBroker.AddFunction(It.IsAny<FunctionLoadRequest>());
            var result = await _functionBroker.InvokeAsync(invocationRequest);
            Assert.Equal(StatusResult.Types.Status.Success, result.Result.Status);
            Assert.True(context.IsDisposed);
        }

        private class TestFunctionExecutionContext : FunctionExecutionContext, IDisposable
        {
            public TestFunctionExecutionContext() { }
            public bool IsDisposed { get; private set; }

            public override RpcTraceContext TraceContext { get; }

            public override InvocationRequest InvocationRequest { get; }

            public override IServiceProvider InstanceServices { get; }

            public override FunctionDescriptor FunctionDescriptor { get; set; }
            public override object InvocationResult { get; set; }
            public override InvocationLogger Logger { get; set; }
            public override List<ParameterBinding> ParameterBindings { get; set; } = new List<ParameterBinding>();
            public override IDictionary<object, object> Items { get; set; }

            public void Dispose()
            {
                IsDisposed = true;
            }
        }
    }
}
