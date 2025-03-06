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
using Microsoft.Azure.Functions.Worker.Tests.Shared;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class FunctionContextExtensionTests
    {
        DefaultFunctionContext _defaultFunctionContext;
        IServiceScopeFactory _serviceScopeFactory;
        IServiceProvider _serviceProvider;
        InvocationFeatures _features;

        public FunctionContextExtensionTests()
        {
            IServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IConverterContextFactory, DefaultConverterContextFactory>();
            serviceCollection.AddSingleton<IBindingCache<ConversionResult>, DefaultBindingCache<ConversionResult>>();

            _serviceProvider = serviceCollection.BuildServiceProvider();
            _serviceScopeFactory = _serviceProvider.GetService<IServiceScopeFactory>();

            _features = new InvocationFeatures(Enumerable.Empty<IInvocationFeatureProvider>());

            var invocation = new Mock<FunctionInvocation>(MockBehavior.Strict).Object;
            _features.Set<FunctionInvocation>(invocation);
        }

        [Fact]
        public async Task BindInputAsyncWorks()
        {
            // Arrange
            var qTriggerFunctionDefinition = new TestFunctionDefinition(parameters: new[]
                                    {
                                        new FunctionParameter("myBook", typeof(string)),
                                        new FunctionParameter("blob1", typeof(Book)),
                                        new FunctionParameter("blob2", typeof(Book))
                                    },
                                    inputBindings: new Dictionary<string, BindingMetadata>
                                    {
                                        { "myBook", new TestBindingMetadata("myBook","queueTrigger",BindingDirection.In) },
                                        { "blob1", new TestBindingMetadata("blob1","blob",BindingDirection.In) },
                                        { "blob2", new TestBindingMetadata("blob2","blob",BindingDirection.In) }
                                    });
            _features.Set<FunctionDefinition>(qTriggerFunctionDefinition);
            _defaultFunctionContext = new DefaultFunctionContext(_serviceScopeFactory, _features, CancellationToken.None);

            var functionBindings = new TestFunctionBindingsFeature
            {
                InputData = new ReadOnlyDictionary<string, object>(
                    new Dictionary<string, object> {
                            { "myBook", "book1" },
                            { "blob1", JsonConvert.SerializeObject(new Book { Title="b1"}) },
                            { "blob2", JsonConvert.SerializeObject(new Book { Title="b2"}) }
                    })
            };
            _features.Set<IFunctionBindingsFeature>(functionBindings);

            // Mock input conversion feature to return a successfully converted value for blob2 input data.
            var conversionFeature = new Mock<IInputConversionFeature>(MockBehavior.Strict);
            conversionFeature.Setup(a =>
                    a.ConvertAsync(It.Is<ConverterContext>(ctx => ctx.Source.ToString().Contains("b2"))))
                             .ReturnsAsync(ConversionResult.Success(new Book { Id = "book 2" }));
            _features.Set<IInputConversionFeature>(conversionFeature.Object);

            // Act
            // bind to the second blob input binding(blob2).
            var blob2InputBinding = _defaultFunctionContext.FunctionDefinition.InputBindings.Values.First(a => a.Name == "blob2");
            var book = (await _defaultFunctionContext.BindInputAsync<Book>(blob2InputBinding)).Value;

            // Assert
            Assert.Equal("book 2", book.Id);
            book.Id = "updated Id value";

            // Make a second call,requesting the result as Object.
            var bindingResult = await _defaultFunctionContext.BindInputAsync<object>(blob2InputBinding);
            var book2 = TestUtility.AssertIsTypeAndConvert<Book>(bindingResult.Value);
            Assert.Equal("updated Id value", book2.Id);

            // Replace the entire result of binding with a different object.
            bindingResult.Value = new Book() {  Id ="book 3"};
            var book3 = (await _defaultFunctionContext.BindInputAsync<Book>(blob2InputBinding)).Value;
            Assert.NotSame(book3, book2);
            Assert.Equal("book 3", book3.Id);
        }

        [Fact]
        public void GetInvocationResult_Works()
        {
            // Arrange
            var qTriggerFunctionDefinition = new TestFunctionDefinition(parameters: new[]
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
                InputData = new ReadOnlyDictionary<string, object>(new Dictionary<string, object> { { "myQueueItem", "bar" } }),
                InvocationResult = new Book { Id = "value set from within the function" }
            };
            _features.Set<IFunctionBindingsFeature>(functionBindings);

            // Act
            InvocationResult<Book> actual1 = _defaultFunctionContext.GetInvocationResult<Book>();
            var book1 = TestUtility.AssertIsTypeAndConvert<Book>(actual1.Value);

            // Also, try the other overload which does not return the result as requested type T.
            InvocationResult actual2 = _defaultFunctionContext.GetInvocationResult();
            var book2 = TestUtility.AssertIsTypeAndConvert<Book>(actual2.Value);

            // Assert
            Assert.NotNull(book1);
            Assert.Equal("value set from within the function", book1.Id);
            Assert.Same(book1, book2);

            // Update the value, call GetInvocationResult again and verify changes.
            book1.Id = "updated value from first middleware";
            var actual3 = _defaultFunctionContext.GetInvocationResult();
            var book3 = TestUtility.AssertIsTypeAndConvert<Book>(actual3.Value);
            Assert.Equal("updated value from first middleware", book3.Id);
            Assert.Same(book1, book3);
        }

        [Fact]
        public void GetInvocationResult_Throws_When_Requesting_With_IncorrectType()
        {
            // Arrange
            var qTriggerFunctionDefinition = new TestFunctionDefinition(parameters: new[]
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
                InputData = new ReadOnlyDictionary<string, object>(new Dictionary<string, object> { { "myQueueItem", "bar" } }),
                InvocationResult = new Book { Id = "value set from within the function" }
            };
            _features.Set<IFunctionBindingsFeature>(functionBindings);

            // Act
            var exception = Assert.Throws<InvalidOperationException>(() => _defaultFunctionContext.GetInvocationResult<int>());
            var expectedExceptionMsg =
                "Requested type(System.Int32) does not match the type of Invocation result(Microsoft.Azure.Functions.Worker.Tests.Book)";
            Assert.Equal(expectedExceptionMsg, exception.Message);
        }

        [Fact]
        public void GetOutputBindingData_Works()
        {
            // Arrange
            var functionDefinition = new TestFunctionDefinition(
            outputBindings: new Dictionary<string, BindingMetadata>
            {
                { "Name", new TestBindingMetadata("Name","queue",BindingDirection.Out) },
                { "HttpResponse", new TestBindingMetadata("HttpResponse","http",BindingDirection.Out) }
            });
            _features.Set<FunctionDefinition>(functionDefinition);

            _defaultFunctionContext = new DefaultFunctionContext(_serviceScopeFactory, _features, CancellationToken.None);

            var functionBindings = new TestFunctionBindingsFeature();
            var grpcHttpResponse = new GrpcHttpResponseData(_defaultFunctionContext, HttpStatusCode.OK);
            functionBindings.OutputBindingData.Add("HttpResponse", grpcHttpResponse);
            functionBindings.OutputBindingData.Add("Name", "some name");
            _features.Set<IFunctionBindingsFeature>(functionBindings);

            // Act
            var httpOutputBinding = _defaultFunctionContext.GetOutputBindings<HttpResponseData>().First(a => a.BindingType == "http");
            var queueOutputBinding = _defaultFunctionContext.GetOutputBindings<object>().First(a => a.BindingType == "queue");

            // Assert
            Assert.NotNull(httpOutputBinding.Value);
            Assert.Same(grpcHttpResponse, httpOutputBinding.Value);

            // Also verify we can do other typical operations like adding a response header.
            httpOutputBinding.Value.Headers.Add("X-Foo-Id", "bar");

            // overwrite the value of one of the outputbinding item
            queueOutputBinding.Value = "foo";

            // Get again and verify previous changes(Adding header) are reflected.
            var actual2 = _defaultFunctionContext.GetOutputBindings<object>().First(a => a.BindingType == "http");
            var httpResponse = TestUtility.AssertIsTypeAndConvert<HttpResponseData>(actual2.Value);
            Assert.Equal("bar", httpResponse.Headers.First(a => a.Key == "X-Foo-Id").Value.First());

            var queueOutputBinding2 = _defaultFunctionContext.GetOutputBindings<object>().First(a => a.BindingType == "queue");
            Assert.Equal("foo", queueOutputBinding2.Value);
        }

        [Fact]
        public void GetOutputBindingData_Works_When_OutputBindingData_Not_set()
        {
            // OutputBindingData won't be set when function fails to execute successfully.

            // Arrange
            var functionDefinition = new TestFunctionDefinition(
            outputBindings: new Dictionary<string, BindingMetadata>
            {
                { "Name", new TestBindingMetadata("Name","queue",BindingDirection.Out) },
                { "HttpResponse", new TestBindingMetadata("HttpResponse","http",BindingDirection.Out) }
            });
            _features.Set<FunctionDefinition>(functionDefinition);

            _defaultFunctionContext = new DefaultFunctionContext(_serviceScopeFactory, _features, CancellationToken.None);

            var functionBindings = new TestFunctionBindingsFeature();
            // Not setting any values to functionBindings.OutputBindingData dictionary.

            _features.Set<IFunctionBindingsFeature>(functionBindings);

            // Act
            var httpOutputBinding = _defaultFunctionContext.GetOutputBindings<HttpResponseData>().First(a => a.BindingType == "http");
            var queueOutputBinding = _defaultFunctionContext.GetOutputBindings<object>().First(a => a.BindingType == "queue");

            // Assert
            Assert.Null(httpOutputBinding.Value);
            Assert.Null(httpOutputBinding.Value);

            // Overwrite the output binding entries with non null values.
            httpOutputBinding.Value = new GrpcHttpResponseData(_defaultFunctionContext, HttpStatusCode.BadRequest);
            queueOutputBinding.Value = "foo";

            // Get again and verify previous changes made to the binding entries are reflected.
            var httpOutputBinding2 = _defaultFunctionContext.GetOutputBindings<object>().First(a => a.BindingType == "http");
            var httpResponse = TestUtility.AssertIsTypeAndConvert<HttpResponseData>(httpOutputBinding2.Value);
            Assert.Equal(HttpStatusCode.BadRequest, httpResponse.StatusCode);

            var queueOutputBinding2 = _defaultFunctionContext.GetOutputBindings<object>().First(a => a.BindingType == "queue");
            Assert.Equal("foo", queueOutputBinding2.Value);
        }
    }
}
