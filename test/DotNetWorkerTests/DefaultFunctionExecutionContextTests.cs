using Microsoft.Azure.Functions.DotNetWorker;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Runtime.Serialization;
using Xunit;

namespace Microsoft.Azure.Functions.DotNetWorkerTests
{
    public class DefaultFunctionExecutionContextTests
    {
        Mock<IServiceScopeFactory> _mockServiceScopeFactory = new Mock<IServiceScopeFactory>(MockBehavior.Strict);
        InvocationRequest _invocationRequest;
        DefaultFunctionExecutionContext _defaultFunctionExecutionContext;

        public DefaultFunctionExecutionContextTests()
        {
            _invocationRequest = new InvocationRequest();
            _defaultFunctionExecutionContext = new DefaultFunctionExecutionContext(_mockServiceScopeFactory.Object, _invocationRequest);
        }

        [Fact]
        public void InstanceServicesCreatedSuccessfullyTest()
        {
            Mock<IServiceScope> instanceServicesScope = new Mock<IServiceScope>();
            Mock<IServiceProvider> serviceProvider = new Mock<IServiceProvider>();
            instanceServicesScope.SetupAllProperties();
            instanceServicesScope.Setup(p => p.ServiceProvider).Returns(serviceProvider.Object);
            _mockServiceScopeFactory.Setup(f => f.CreateScope()).Returns(instanceServicesScope.Object);
            var services = _defaultFunctionExecutionContext.InstanceServices;
            Assert.NotNull(services);
        }

        [Fact]
        public void InstanceServicesDisposedSuccessfullyTest()
        {
            _defaultFunctionExecutionContext.Dispose();
        }
    }

}
