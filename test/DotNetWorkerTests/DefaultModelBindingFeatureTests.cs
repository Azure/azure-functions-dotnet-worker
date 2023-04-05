using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Tests.Features;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class DefaultModelBindingFeatureTests : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly DefaultFunctionInputBindingFeature _functionInputBindingFeature;
       
        public DefaultModelBindingFeatureTests()
        {

            var serializer = new JsonObjectSerializer(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            _serviceProvider = TestUtility.GetServiceProviderWithInputBindingServices(o => o.Serializer = serializer);
            _functionInputBindingFeature = _serviceProvider.GetService<DefaultFunctionInputBindingFeature>();
        }

        [Fact]
        public async void BindFunctionInputAsync_Populates_ParametersUsingConverters()
        {
            // Arrange
            var parameters = new List<FunctionParameter>()
            {
                new("myQueueItem",typeof(Book)),
                new ("myGuid", typeof(Guid))
            };

            IInvocationFeatures features = new InvocationFeatures(Enumerable.Empty<IInvocationFeatureProvider>());
            features.Set(_serviceProvider.GetService<IInputConversionFeature>());
            features.Set<IFunctionBindingsFeature>(new TestFunctionBindingsFeature()
            {
                InputData = new Dictionary<string, object>
                {
                    { "myQueueItem","{\"id\":\"foo\", \"title\":\"bar\"}" },
                    { "myGuid","0ab4800e-1308-4e9f-be5f-4372717e68eb" }
                }
            });

            var definition = new TestFunctionDefinition(parameters: parameters, inputBindings: new Dictionary<string, BindingMetadata>
            {
                { "myQueueItem", new TestBindingMetadata("myQueueItem","queueTrigger",BindingDirection.In) },
                { "myGuid", new TestBindingMetadata("myGuid","queueTrigger",BindingDirection.In) }
            });
            var functionContext = new TestFunctionContext(definition, invocation: null, CancellationToken.None, serviceProvider: _serviceProvider, features: features);

            // Act
            var bindingResult = await _functionInputBindingFeature.BindFunctionInputAsync(functionContext);
            var parameterValuesArray = bindingResult.Values;
            // Assert
            var book = TestUtility.AssertIsTypeAndConvert<Book>(parameterValuesArray[0]);
            Assert.Equal("foo", book.Id);
            var guid = TestUtility.AssertIsTypeAndConvert<Guid>(parameterValuesArray[1]);
            Assert.Equal("0ab4800e-1308-4e9f-be5f-4372717e68eb", guid.ToString());
        }


        [Fact]
        public async void BindFunctionInputAsync_Populates_SecondParameterHttpRequestData()
        {
            // Arrange
            var parameters = new List<FunctionParameter>
            {
                new("book", typeof(Book)),
                new("req", typeof(HttpRequestData))
            };

            IInvocationFeatures features = new InvocationFeatures(Enumerable.Empty<IInvocationFeatureProvider>());
            features.Set(_serviceProvider.GetService<IInputConversionFeature>());

            var definition = new TestFunctionDefinition(parameters: parameters, inputBindings: new Dictionary<string, BindingMetadata>
            {
                { "book", new TestBindingMetadata("book", "httpTrigger", BindingDirection.In) },
                { "req", new TestBindingMetadata("req", "httpTrigger", BindingDirection.In) }
            });

            var functionContext = new TestFunctionContext(definition, invocation: null, CancellationToken.None, serviceProvider: _serviceProvider, features: features);
            var source = "{\"id\":\"foo\", \"title\":\"bar\"}";
            var request = new TestHttpRequestData(functionContext, new MemoryStream(Encoding.UTF8.GetBytes(source)));

            features.Set<IFunctionBindingsFeature>(new TestFunctionBindingsFeature()
            {
                InputData = new Dictionary<string, object>
                {
                    { "book", request }
                }
            });

            // Act
            var bindingResult = await _functionInputBindingFeature.BindFunctionInputAsync(functionContext);
            var parameterValuesArray = bindingResult.Values;

            // Assert
            var book = TestUtility.AssertIsTypeAndConvert<Book>(parameterValuesArray[0]);
            Assert.Equal("foo", book.Id);

            TestUtility.AssertIsTypeAndConvert<HttpRequestData>(parameterValuesArray[1]);
        }

        [Fact]
        public async void BindFunctionInputAsync_Returns_Cached_Value_When_Called_SecondTime()
        {
            // Arrange
            var parameters = new List<FunctionParameter>()
            {
                new("myQueueItem",typeof(Book))
            };
            IInvocationFeatures features = new InvocationFeatures(Enumerable.Empty<IInvocationFeatureProvider>());
            features.Set(_serviceProvider.GetService<IInputConversionFeature>());
            features.Set<IFunctionBindingsFeature>(new TestFunctionBindingsFeature()
            {
                InputData = new Dictionary<string, object>
                {
                    { "myQueueItem","{\"id\":\"foo\"}" }
                }
            });

            var definition = new TestFunctionDefinition(parameters: parameters, inputBindings: new Dictionary<string, BindingMetadata>
            {
                { "myQueueItem", new TestBindingMetadata("myQueueItem","queueTrigger",BindingDirection.In) }
            });
            var functionContext = new TestFunctionContext(definition, invocation: null, CancellationToken.None, serviceProvider: _serviceProvider, features: features);

            // Act
            var bindingResult1 = await _functionInputBindingFeature.BindFunctionInputAsync(functionContext);
            var parameterValuesArray = bindingResult1.Values;
            // Assert
            var book = TestUtility.AssertIsTypeAndConvert<Book>(parameterValuesArray[0]);
            Assert.Equal("foo", book.Id);

            // Update the result from caller side.
            bindingResult1.Values[0] = new Book { Id = "bar" };
            // Call Bind again. This should return the same result(bindingResult1) instead of rebinding everything from scratch.
            var bindingResult2 = await _functionInputBindingFeature.BindFunctionInputAsync(functionContext);
            Assert.Same(bindingResult1, bindingResult2);
        }

        /// <summary>
        /// This UT simulates a case where the input binding entry is being updated (could be in a middleware) using the
        /// BindInputAsync extension method result and that change is reflected when the FunctionInputBindingFeature
        /// returns the parameter values array for the function definition.
        /// </summary>
        [Fact]
        public async void BindFunctionInputAsync_Populates_ParametersUsingCachedData()
        {
            // Arrange
            var parameters = new List<FunctionParameter>()
            {
                new("myQueueItem",typeof(Book))
            };
            IInvocationFeatures features = new InvocationFeatures(Enumerable.Empty<IInvocationFeatureProvider>());
            features.Set(_serviceProvider.GetService<IInputConversionFeature>());
            features.Set<IFunctionBindingsFeature>(new TestFunctionBindingsFeature()
            {
                InputData = new Dictionary<string, object>
                {
                    { "myQueueItem","{\"title\":\"foo\"}" }
                }
            });

            var definition = new TestFunctionDefinition(parameters: parameters, inputBindings: new Dictionary<string, BindingMetadata>
            {
                { "myQueueItem", new TestBindingMetadata("myQueueItem","queueTrigger",BindingDirection.In) }
            });
            var functionContext = new TestFunctionContext(definition, invocation: null, CancellationToken.None, serviceProvider: _serviceProvider, features: features);

            // Act
            // bind to the queue trigger input binding item.
            var queueBindingMetaData = functionContext.FunctionDefinition
                                                      .InputBindings.Values
                                                      .First(a => a.Type == "queueTrigger");

            var bookInputBindingData = await functionContext.BindInputAsync<Book>(queueBindingMetaData);

            // Assert
            Assert.Equal("foo", bookInputBindingData.Value!.Title);

            // Update this input binding entry value to a different object.
            // This action is similar to the use case where we update an input binding entry value from a middleware.
            var otherBook = new Book { Title = "totally different book" };
            bookInputBindingData.Value = otherBook;

            // Bind a second time using the same extension method. This should return as the updated object.
            var bookInputBindingData2 = await functionContext.BindInputAsync<Book>(queueBindingMetaData);
            Assert.Same(otherBook, bookInputBindingData2.Value);

            // Get all parameters from FunctionInputBindingFeature. This should also reflect what we set above.
            var bindingResult = await _functionInputBindingFeature.BindFunctionInputAsync(functionContext);
            Assert.Same(otherBook, bindingResult.Values[0] as Book);
        }

        public void Dispose()
        {
            _serviceProvider?.Dispose();
            _functionInputBindingFeature?.Dispose();
        }
    }
}
