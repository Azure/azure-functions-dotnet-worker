using Moq;
using Xunit;
using Microsoft.Azure.Functions.DotNetWorker;
using Microsoft.Azure.Functions.DotNetWorker.Pipeline;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;
using System.Threading.Tasks;
using System;
using Microsoft.Azure.Functions.DotNetWorker.Logging;
using System.Collections.Generic;

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
            TestFunctionExecutionContext context = new TestFunctionExecutionContext();
            _mockFunctionExecutionContextFactory.Setup(p => p.Create(It.IsAny<InvocationRequest>())).Returns(context);
            _mockFunctionExecutionDelegate.Setup(p => p(It.IsAny<FunctionExecutionContext>())).Returns(Task.CompletedTask);
            var result = await _functionBroker.InvokeAsync(invocationRequest);
            Assert.True(context.IsDisposed);
        }

        private class TestFunctionExecutionContext: FunctionExecutionContext, IDisposable
        {
            public TestFunctionExecutionContext() { }
            public bool IsDisposed { get; private set; }

            public override RpcTraceContext TraceContext => throw new NotImplementedException();

            public override InvocationRequest InvocationRequest => throw new NotImplementedException();

            public override IServiceProvider InstanceServices => throw new NotImplementedException();

            public override FunctionDescriptor FunctionDescriptor { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public override object InvocationResult { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public override InvocationLogger Logger { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public override List<ParameterBinding> ParameterBindings { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public override IDictionary<object, object> Items { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public void Dispose()
            {
                IsDisposed = true;
            }
        }
    }
}
