using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
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
        public async Task BindFunctionInputAsync_Populates_ParametersUsingConverters()
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
        public async Task BindFunctionInputAsync_IgnoreCancellationToken()
        {
            // Arrange
            var parameters = new List<FunctionParameter>()
            {
                new("myQueueItem",typeof(Book)),
                new ("myGuid", typeof(Guid)),
                new ("token", typeof(CancellationToken))
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

            // Register a cancellation token
            var cts = new CancellationTokenSource();
            cts.Cancel();
            var functionContext = new TestFunctionContext(definition, invocation: null, cts.Token, serviceProvider: _serviceProvider, features: features);

            // Act
            var bindingResult = await _functionInputBindingFeature.BindFunctionInputAsync(functionContext);
            var parameterValuesArray = bindingResult.Values;
            // Assert
            var book = TestUtility.AssertIsTypeAndConvert<Book>(parameterValuesArray[0]);
            Assert.Equal("foo", book.Id);
            var guid = TestUtility.AssertIsTypeAndConvert<Guid>(parameterValuesArray[1]);
            Assert.Equal("0ab4800e-1308-4e9f-be5f-4372717e68eb", guid.ToString());
            var cancellationToken = TestUtility.AssertIsTypeAndConvert<CancellationToken>(parameterValuesArray[2]);
            Assert.True(cancellationToken.IsCancellationRequested);
        }

        [Fact]
        public async Task BindFunctionInputAsync_Populates_Parameter_Using_DefaultValue_When_CouldNot_Populate_From_InputData()
        {
            var features = new InvocationFeatures(Enumerable.Empty<IInvocationFeatureProvider>());
            var httpFunctionDefinition = new TestFunctionDefinition(parameters: new FunctionParameter[]
            {
                 new("req", typeof(HttpRequestData)),
                 new("fooId", typeof(int), true,  defaultValue: 100),
                 new("bar", typeof(string), true, defaultValue: null)
            },
            inputBindings: new Dictionary<string, BindingMetadata>
            {
                 { "req", new TestBindingMetadata("req","httpTrigger",BindingDirection.In) }
            }); 
            features.Set<FunctionDefinition>(httpFunctionDefinition);

            var functionContext = new TestFunctionContext(httpFunctionDefinition, invocation: null, CancellationToken.None, serviceProvider: _serviceProvider, features: features);
            var grpcHttpReq = new GrpcHttpRequestData(TestUtility.CreateRpcHttp(), functionContext);
            var functionBindings = new TestFunctionBindingsFeature
            {
                InputData = new ReadOnlyDictionary<string, object>(new Dictionary<string, object> { { "req", grpcHttpReq } })
            };
            features.Set<IFunctionBindingsFeature>(functionBindings);
            features.Set(_serviceProvider.GetService<IInputConversionFeature>());

            // Act
            var bindingResult = await _functionInputBindingFeature.BindFunctionInputAsync(functionContext);
            var parameterValuesArray = bindingResult.Values;

            // Assert
            var httpReqData = TestUtility.AssertIsTypeAndConvert<HttpRequestData>(parameterValuesArray[0]);
            Assert.NotNull(httpReqData);
            var fooId = TestUtility.AssertIsTypeAndConvert<int>(parameterValuesArray[1]);
            Assert.Equal(100, fooId);
            var bar = parameterValuesArray[2];
            Assert.Null(bar);
        }

        [Fact]
        public async Task BindFunctionInputAsync_Populates_Parameter_For_Nullable_Or_ReferenceTypes()
        {
            var features = new InvocationFeatures(Enumerable.Empty<IInvocationFeatureProvider>());
            var httpFunctionDefinition = new TestFunctionDefinition(parameters: new FunctionParameter[]
            {
                 new("req", typeof(HttpRequestData)),
                 new("bar", typeof(string)),
                 new("fooId", typeof(int?)),
                 new("bazDate", typeof(DateTime?))
            },
            inputBindings: new Dictionary<string, BindingMetadata>
            {
                 { "req", new TestBindingMetadata("req","httpTrigger",BindingDirection.In) }
             }); 
            features.Set<FunctionDefinition>(httpFunctionDefinition);

            var functionContext = new TestFunctionContext(httpFunctionDefinition, invocation: null, CancellationToken.None, serviceProvider: _serviceProvider, features: features);
            var grpcHttpReq = new GrpcHttpRequestData(TestUtility.CreateRpcHttp(), functionContext);
            var functionBindings = new TestFunctionBindingsFeature
            {
                InputData = new ReadOnlyDictionary<string, object>(new Dictionary<string, object> { { "req", grpcHttpReq } })
            };
            features.Set<IFunctionBindingsFeature>(functionBindings);
            features.Set(_serviceProvider.GetService<IInputConversionFeature>());

            // Act
            var bindingResult = await _functionInputBindingFeature.BindFunctionInputAsync(functionContext);
            var parameterValuesArray = bindingResult.Values;

            // Assert
            var httpReqData = TestUtility.AssertIsTypeAndConvert<HttpRequestData>(parameterValuesArray[0]);
            Assert.NotNull(httpReqData);

            // The input binding data does not have values for "bar","fooId" and "bazDate"
            // but since they are nullable or reference types, they should be populated with null.
            var bar = parameterValuesArray[1];
            Assert.Null(bar);
            var fooId = parameterValuesArray[2];
            Assert.Null(fooId);
            var bazDate = parameterValuesArray[3];
            Assert.Null(bazDate);
        }

        [Fact]
        public async Task BindFunctionInputAsync_Throws_When_Explicit_OptionalParametersValueNotPresent()
        {
            // 'fooId' is a parameter defined for the function without a default value
            // and 'InputData' does not have a corresponding entry for that we could use to populate that parameter.

            IInvocationFeatures features = new InvocationFeatures(Enumerable.Empty<IInvocationFeatureProvider>());
            var httpFunctionDefinition = new TestFunctionDefinition(parameters: new FunctionParameter[]
            {
                 new("req", typeof(HttpRequestData)),
                 new("fooId", typeof(int))
            },
            inputBindings: new Dictionary<string, BindingMetadata>
            {
                 { "req", new TestBindingMetadata("req","httpTrigger",BindingDirection.In) }
            }); 
            features.Set<FunctionDefinition>(httpFunctionDefinition);

            var functionContext = new TestFunctionContext(httpFunctionDefinition, invocation: null, CancellationToken.None, serviceProvider: _serviceProvider, features: features);
            var grpcHttpReq = new GrpcHttpRequestData(TestUtility.CreateRpcHttp(), functionContext);
            var functionBindings = new TestFunctionBindingsFeature
            {
                InputData = new ReadOnlyDictionary<string, object>(new Dictionary<string, object> { { "req", grpcHttpReq } })
            };
            features.Set<IFunctionBindingsFeature>(functionBindings);
            features.Set(_serviceProvider.GetService<IInputConversionFeature>());

            // Assert
            var exception = await Assert.ThrowsAsync<FunctionInputConverterException>(async () => await _functionInputBindingFeature.BindFunctionInputAsync(functionContext));
            Assert.Equal("Error converting 1 input parameters for Function 'TestName': Could not populate the value for 'fooId' parameter. Consider adding a default value or making the parameter nullable.", exception.Message);
        }

        [Fact]
        public async Task BindFunctionInputAsync_Returns_Cached_Value_When_Called_SecondTime()
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
        public async Task BindFunctionInputAsync_Populates_ParametersUsingCachedData()
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
