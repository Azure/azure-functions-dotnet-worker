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
        InvocationRequest _invocationRequest;
        DefaultFunctionExecutionContext _defaultFunctionExecutionContext;
        IServiceScopeFactory _serviceScopeFactory;
        IServiceProvider _serviceProvider;

        public DefaultFunctionExecutionContextTests()
        {
            IServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<SingletonService>();
            serviceCollection.AddTransient<TransientService>();
            serviceCollection.AddScoped<ScopedService>();
            _serviceProvider = serviceCollection.BuildServiceProvider();
            _serviceScopeFactory = _serviceProvider.GetService<IServiceScopeFactory>();
            _invocationRequest = new InvocationRequest();
            _defaultFunctionExecutionContext = new DefaultFunctionExecutionContext(_serviceScopeFactory, _invocationRequest);
        }

        [Fact]
        public void InstanceServicesCreatedSuccessfullyTest()
        {
            var services = _defaultFunctionExecutionContext.InstanceServices;
            Assert.NotNull(services);
            Assert.NotNull(services.GetService<SingletonService>());
            Assert.NotNull(services.GetService<TransientService>());
            Assert.NotNull(services.GetService<ScopedService>());
        }

        [Fact]
        public void InstanceServicesDisposedSuccessfullyTest()
        {
            var services = _defaultFunctionExecutionContext.InstanceServices;
            _defaultFunctionExecutionContext.Dispose();
            Assert.Throws<ObjectDisposedException>(services.GetService<SingletonService>);
            Assert.Throws<ObjectDisposedException>(services.GetService<TransientService>);
            Assert.Throws<ObjectDisposedException>(services.GetService<ScopedService>);
        }

        // service classes for testing
        private class SingletonService : IDisposable
        {
            public SingletonService() { }
            public bool IsDisposed { get; private set; }
            public void Dispose()
            {
                IsDisposed = true;
            }
        }
        private class TransientService : SingletonService { }
        private class ScopedService : SingletonService { }
    }
}
