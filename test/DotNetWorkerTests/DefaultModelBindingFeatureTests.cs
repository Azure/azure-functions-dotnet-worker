using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class DefaultModelBindingFeatureTests
    {
        private readonly DefaultModelBindingFeature _modelBindingFeature;

        public DefaultModelBindingFeatureTests()
        {
            // Test overriding serialization settings.
            var serializer = new JsonObjectSerializer(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            _modelBindingFeature = TestUtility.GetDefaultModelBindingFeature(o => o.Serializer = serializer);
        }
        [Fact]
        public async void Foo()
        {
            var definition = new TestFunctionDefinition();
            var functionContext = new TestFunctionContext(definition, null);
            var actual = await _modelBindingFeature.BindFunctionInputAsync(functionContext);

            Assert.NotNull(actual);
        }

    }
}
