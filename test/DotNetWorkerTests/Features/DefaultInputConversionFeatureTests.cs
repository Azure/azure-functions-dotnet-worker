// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Core.Converters;
using Microsoft.Azure.Functions.Worker.Core.Converters.Converter;
using Moq;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests.Features
{
    public class DefaultInputConversionFeatureTests
    {
        private readonly Mock<IInputConverterProvider> _mockIinputConverterProvider = new(MockBehavior.Strict);

        public DefaultInputConversionFeatureTests()
        {
            _mockIinputConverterProvider.Setup(a => a.DefaultConverters)
                            .Returns(new List<IInputConverter>
                                   {
                                       new TypeConverter() ,
                                       new GuidConverter() 
                                   });
            _mockIinputConverterProvider.Setup(a => a.GetOrCreateConverterInstance(typeof(MyTestSyncInputConverter)))
                                      .Returns(new MyTestSyncInputConverter());
                        
            _mockIinputConverterProvider.Setup(a => a.GetOrCreateConverterInstance(typeof(MyCustomerAsyncInputConverter)))
                                      .Returns(new MyCustomerAsyncInputConverter());
        }

        [Fact]
        public async Task Convert_Using_Default_Converters()
        {
            var inputConversionFeature = new DefaultInputConversionFeature(_mockIinputConverterProvider.Object);
            var converterContext = CreateConverterContext(typeof(Guid), "0c67c078-7213-4e91-ad41-f8747c865f3d");

            var actual = await inputConversionFeature.ConvertAsync(converterContext);

            Assert.True(actual.IsSuccess);
            TestUtility.AssertIsTypeAndConvert<Guid>(actual.Model);
            Assert.Equal("0c67c078-7213-4e91-ad41-f8747c865f3d", actual.Model.ToString());
        }

        [Fact]
        public async Task Convert_Using_Converter_Specified_In_ConverterContext_Properties()
        {
            var inputConversionFeature = new DefaultInputConversionFeature(_mockIinputConverterProvider.Object);
            var converterContext = CreateConverterContext(typeof(Guid), "0c67c078-7213-4e91-ad41-f8747c865f3d");
            // Explicitly specifiy a converter to be used.
            converterContext.Properties = new Dictionary<string, object>()
            {
                { PropertyBagKeys.ConverterType, typeof(MyTestSyncInputConverter).AssemblyQualifiedName }
            };

            var actual = await inputConversionFeature.ConvertAsync(converterContext);

            Assert.True(actual.IsSuccess);
            Assert.Equal("0c67c078-7213-4e91-ad41-f8747c865f3d-converted value", actual.Model);
            TestUtility.AssertIsTypeAndConvert<string>(actual.Model);
        }
                
        [Fact]
        public async Task Convert_Using_Converter_From_InputConverterAttribute_Of_TargetType()
        {
            var inputConversionFeature = new DefaultInputConversionFeature(_mockIinputConverterProvider.Object);
            var converterContext = CreateConverterContext(typeof(Customer), "16");           

            var actual = await inputConversionFeature.ConvertAsync(converterContext);

            Assert.True(actual.IsSuccess);
            var customer = TestUtility.AssertIsTypeAndConvert<Customer>(actual.Model);
            Assert.Equal("16-converted value", customer.Name);
        }

        [InputConverter(typeof(MyCustomerAsyncInputConverter))]
        internal record Customer(string Id, string Name);
                
        internal class MyCustomerAsyncInputConverter : IInputConverter
        {
            public async ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
            {
                await Task.Delay(1);  // simulate an async operation.
                var customer = new Customer(context.Source.ToString(), context.Source + "-converted value");

                return ConversionResult.Success(model: customer);
            }
        }

        internal class MyTestSyncInputConverter : IInputConverter
        {
            public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
            {
                var result = ConversionResult.Success(model: context.Source + "-converted value");

                return new ValueTask<ConversionResult>(result);
            }
        }
                
        private DefaultConverterContext CreateConverterContext(Type targetType, object source)
        {
            var definition = new TestFunctionDefinition();
            var functionContext = new TestFunctionContext(definition, null);

            return new DefaultConverterContext(targetType, source, functionContext);
        }
    }
}
