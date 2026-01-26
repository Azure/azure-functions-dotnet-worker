// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class FunctionContextHttpExtensionTests
    {
        DefaultFunctionContext _defaultFunctionContext;
        IServiceScopeFactory _serviceScopeFactory;
        IServiceProvider _serviceProvider;
        InvocationFeatures _features;
        Mock<IInputConversionFeature> _mockConversionFeature;

        public FunctionContextHttpExtensionTests()
        {
            IServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IConverterContextFactory, DefaultConverterContextFactory>();
            serviceCollection.AddSingleton<IBindingCache<ConversionResult>, DefaultBindingCache<ConversionResult>>();

            _serviceProvider = serviceCollection.BuildServiceProvider();
            _serviceScopeFactory = _serviceProvider.GetService<IServiceScopeFactory>();

            _features = new InvocationFeatures(Enumerable.Empty<IInvocationFeatureProvider>());
            _features.Set(new Mock<FunctionInvocation>(MockBehavior.Strict).Object);

            _mockConversionFeature = new Mock<IInputConversionFeature>(MockBehavior.Strict);
        }

        [Fact]
        public async Task GetHttpRequestDataAsync_Returns_Null_For_NonHttp_Invocation()
        {
            // Arrange
            var qTriggerFunctionDefinition = new TestFunctionDefinition(parameters: new FunctionParameter[]
            {
                new FunctionParameter("myQueueItem", typeof(string))
            },
            inputBindings: new Dictionary<string, BindingMetadata>
            {
                { "myQueueItem", new TestBindingMetadata("myQueueItem","queueTrigger",BindingDirection.In) }
            });
            _features.Set<FunctionDefinition>(qTriggerFunctionDefinition);

            _defaultFunctionContext = new DefaultFunctionContext(_serviceScopeFactory, _features, CancellationToken.None);
            var functionBindings = new TestFunctionBindingsFeature
            {
                InputData = new ReadOnlyDictionary<string, object>(new Dictionary<string, object> { { "myQueueItem", "foo" } })
            };
            _features.Set<IFunctionBindingsFeature>(functionBindings);

            // Act
            var actual = await _defaultFunctionContext.GetHttpRequestDataAsync();

            // Assert
            Assert.Null(actual);
        }

        [Fact]
        public async Task GetHttpRequestDataAsync_Works_For_Http_Invocation()
        {
            // Arrange
            var httpFunctionDefinition = new TestFunctionDefinition(parameters: new FunctionParameter[]
            {
                new FunctionParameter("req", typeof(HttpRequestData))
            },
            inputBindings: new Dictionary<string, BindingMetadata>
            {
                { "req", new TestBindingMetadata("req","httpTrigger",BindingDirection.In) }
            });
            _features.Set<FunctionDefinition>(httpFunctionDefinition);

            _defaultFunctionContext = new DefaultFunctionContext(_serviceScopeFactory, _features, CancellationToken.None);
            var grpcHttpReq = new GrpcHttpRequestData(TestUtility.CreateRpcHttp(), _defaultFunctionContext);
            var functionBindings = new TestFunctionBindingsFeature
            {
                InputData = new ReadOnlyDictionary<string, object>(new Dictionary<string, object> { { "req", grpcHttpReq } })
            };
            _features.Set<IFunctionBindingsFeature>(functionBindings);

            // Mock input conversion feature to return a successfully converted HttpRequestData value.
            _mockConversionFeature.Setup(a => a.ConvertAsync(It.IsAny<ConverterContext>()))
                                  .ReturnsAsync(ConversionResult.Success(grpcHttpReq));
            _features.Set<IInputConversionFeature>(_mockConversionFeature.Object);

            // Act
            var actual1 = await _defaultFunctionContext.GetHttpRequestDataAsync();
            var actual2 = await _defaultFunctionContext.GetHttpRequestDataAsync();

            // Assert
            Assert.NotNull(actual1);
            Assert.NotNull(actual1.Url);
            Assert.Equal(new[] { "gzip", "deflate" }, actual1.Headers.First(a => a.Key == "Accept-Encoding").Value);
            Assert.Equal("light", actual1.Cookies.First(x => x.Name == "theme").Value);

            // Calling "GetHttpRequestDataAsync" again should return same object(cached value).
            Assert.Same(actual1, actual2);
        }

        [Fact]
        public void GetHttpResponseData_Works_For_Http_Invocation()
        {
            // Arrange
            _features.Set<FunctionDefinition>(new TestFunctionDefinition());

            _defaultFunctionContext = new DefaultFunctionContext(_serviceScopeFactory, _features, CancellationToken.None);
            var grpcHttpReq = new GrpcHttpRequestData(TestUtility.CreateRpcHttp(), _defaultFunctionContext);
            var functionBindings = new TestFunctionBindingsFeature
            {
                InputData = new ReadOnlyDictionary<string, object>(new Dictionary<string, object> { { "req", grpcHttpReq } }),
                InvocationResult = new GrpcHttpResponseData(_defaultFunctionContext, HttpStatusCode.OK)
            };
            _features.Set<IFunctionBindingsFeature>(functionBindings);

            // Act
            HttpResponseData actual = _defaultFunctionContext.GetHttpResponseData();

            // Assert
            Assert.NotNull(actual);
            Assert.Equal(HttpStatusCode.OK, actual.StatusCode);

            // Also verify we can do other typical operations like adding a response header.
            actual.Headers.Add("X-Foo-Id", "bar");

            // Call the GetHttpResponseData method again and verify.
            HttpResponseData actual2 = _defaultFunctionContext.GetHttpResponseData();
            Assert.True(actual2.Headers.First(a => a.Key == "X-Foo-Id").Value.Any());
        }

        [Fact]
        public void GetHttpResponseData_Works_For_Http_Invocation_POCO_OutputBinding_Properties()
        {
            // Arrange
            _features.Set<FunctionDefinition>(new TestFunctionDefinition(
            outputBindings: new Dictionary<string, BindingMetadata>
            {
                { "MyName", new TestBindingMetadata("MyName","queue",BindingDirection.Out) },
                { "MyHttpResponse", new TestBindingMetadata("MyHttpResponse","http",BindingDirection.Out) }
            }));

            _defaultFunctionContext = new DefaultFunctionContext(_serviceScopeFactory, _features, CancellationToken.None);
            var grpcHttpReq = new GrpcHttpRequestData(TestUtility.CreateRpcHttp(), _defaultFunctionContext);
            var functionBindings = new TestFunctionBindingsFeature
            {
                InputData = new ReadOnlyDictionary<string, object>(new Dictionary<string, object> { { "req", grpcHttpReq } })
            };

            // Set the outputbinding data of the function invocation.
            // In the normal(non unit test) run of the worker, this values will be set by our execution middleware pipeline.
            functionBindings.OutputBindingData.Add("MyHttpResponse", new GrpcHttpResponseData(_defaultFunctionContext, HttpStatusCode.OK));
            functionBindings.OutputBindingData.Add("MyName", "foo");
            _features.Set<IFunctionBindingsFeature>(functionBindings);

            // Act
            HttpResponseData actual = _defaultFunctionContext.GetHttpResponseData();

            // Assert
            Assert.NotNull(actual);
            Assert.Equal(HttpStatusCode.OK, actual.StatusCode);

            // Also verify we can do other typical operations like adding a response header.
            actual.Headers.Add("X-Foo-Id", "bar");

            // Call the GetHttpResponseData method again and verify.
            HttpResponseData actual2 = _defaultFunctionContext.GetHttpResponseData();
            Assert.True(actual2.Headers.First(a => a.Key == "X-Foo-Id").Value.Any());
        }
    }
}
