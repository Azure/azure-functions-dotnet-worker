// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Core.Converters;
using Microsoft.Azure.Functions.Worker.Core.Converters.Converter;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests.Features
{
    public sealed class DefaultInputConversionFeatureTests
    {
        private readonly DefaultInputConversionFeature _defaultInputConversionFeature;

        public DefaultInputConversionFeatureTests()
        {
            // Test overriding serialization settings.
            var serializer = new JsonObjectSerializer(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            _defaultInputConversionFeature = TestUtility.GetDefaultInputConversionFeature(o => o.Serializer = serializer);
        }

        [Fact]
        public async Task Convert_Using_Default_Converters_JSON_Poco()
        {
            // Simulate Cosmos for POCO with case insensitive json
            var source =
                 @"[
                    { ""id"": ""1"", ""author"": ""a"", ""title"": ""b"" },
                    { ""id"": ""2"", ""author"": ""c"", ""title"": ""d"" },
                    { ""id"": ""3"", ""author"": ""e"", ""title"": ""f"" }
                  ]";
            var converterContext = CreateConverterContext(typeof(IReadOnlyList<Book>), source);

            var actual = await _defaultInputConversionFeature.ConvertAsync(converterContext);

            Assert.True(actual.IsSuccess);
            var targetEnum = TestUtility.AssertIsTypeAndConvert<IReadOnlyList<Book>>(actual.Model);
            Assert.Collection(targetEnum,
                p => Assert.True(p.Id == "1" && p.Author == "a"),
                p => Assert.True(p.Id == "2" && p.Author == "c"),
                p => Assert.True(p.Id == "3" && p.Author == "e"));
        }        

        [Fact]
        public async Task Convert_Using_Default_Converters_Guid()
        {
            var converterContext = CreateConverterContext(typeof(Guid), "0c67c078-7213-4e91-ad41-f8747c865f3d");

            var actual = await _defaultInputConversionFeature.ConvertAsync(converterContext);

            Assert.True(actual.IsSuccess);
            TestUtility.AssertIsTypeAndConvert<Guid>(actual.Model);
            Assert.Equal("0c67c078-7213-4e91-ad41-f8747c865f3d", actual.Model.ToString());
        }

        [Fact]
        public async Task Convert_Using_Converter_Specified_In_ConverterContext_Properties()
        {
            var converterContext = CreateConverterContext(typeof(string), "0c67c078-7213-4e91-ad41-f8747c865f3d");
            // Explicitly specify a converter to be used via ConverterContext.Properties.
            converterContext.Properties = new Dictionary<string, object>()
            {
                { PropertyBagKeys.ConverterType, typeof(MySimpleSyncInputConverter).AssemblyQualifiedName }
            };

            var actual = await _defaultInputConversionFeature.ConvertAsync(converterContext);

            Assert.True(actual.IsSuccess);
            Assert.Equal("0c67c078-7213-4e91-ad41-f8747c865f3d-converted value", actual.Model);
            TestUtility.AssertIsTypeAndConvert<string>(actual.Model);
        }

        [Fact]
        public async Task Convert_Using_Converter_From_InputConverterAttribute_Of_TargetType()
        {
            // Target type used is "Customer" which has an "InputConverter" decoration
            // to use the "MyCustomerAsyncInputConverter" converter.
            var converterContext = CreateConverterContext(typeof(Customer), "16");

            var actual = await _defaultInputConversionFeature.ConvertAsync(converterContext);

            Assert.True(actual.IsSuccess);
            var customer = TestUtility.AssertIsTypeAndConvert<Customer>(actual.Model);
            Assert.Equal("16-converted customer", customer.Name);
        }

        [InputConverter(typeof(MyCustomerAsyncInputConverter))]
        internal record Customer(string Id, string Name);

        internal class MyCustomerAsyncInputConverter : IInputConverter
        {
            public async ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
            {
                await Task.Delay(1);  // simulate an async operation.
                var customer = new Customer(context.Source.ToString(), context.Source + "-converted customer");

                return ConversionResult.Success(model: customer);
            }
        }

        internal class MySimpleSyncInputConverter : IInputConverter
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
