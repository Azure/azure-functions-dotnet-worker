using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Azure.Functions.Worker.Tests.Features;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class DefaultModelBindingFeatureTests
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly DefaultModelBindingFeature _modelBindingFeature;
        public DefaultModelBindingFeatureTests()
        {
            var serializer = new JsonObjectSerializer(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            _serviceProvider = TestUtility.GetServiceProviderWithInputBindingServices(o => o.Serializer = serializer);
            _modelBindingFeature = _serviceProvider.GetService<DefaultModelBindingFeature>();
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
            var parameterValuesArray = await _modelBindingFeature.BindFunctionInputAsync(functionContext);

            // Assert
            var book = TestUtility.AssertIsTypeAndConvert<Book>(parameterValuesArray[0]);
            Assert.Equal("foo", book.Id);
            var guid = TestUtility.AssertIsTypeAndConvert<Guid>(parameterValuesArray[1]);
            Assert.Equal("0ab4800e-1308-4e9f-be5f-4372717e68eb", guid.ToString());
        }

        /// <summary>
        /// This UT simulates a case where the input binding entry is being updated (could be in a middleware) using the
        /// BindInputAsync extension method result and that change is reflected when the ModelBindingFeature
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

            // Get all parameters from ModelBindingFeature. This should also reflect what we set above.
            var parameterValuesArray = await _modelBindingFeature.BindFunctionInputAsync(functionContext);
            Assert.Same(otherBook, parameterValuesArray[0] as Book);
        }
    }
}
