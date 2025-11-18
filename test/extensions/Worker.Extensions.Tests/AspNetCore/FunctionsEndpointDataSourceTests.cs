using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Routing;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.AspNetMiddleware;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests.AspNetCore
{
    public class FunctionsEndpointDataSourceTests
    {
        [Theory]
        [InlineData("api")]
        [InlineData("customRoutePrefix")]
        public void MapHttpFunction(string routePrefix)
        {
            string rawBinding = """
            {
                "name": "req",
                "direction": "In",
                "Type": "httpTrigger",
                "authLevel": "Anonymous",
                "methods": ["get", "post"],
                "properties": { }
            }
            """;

            var metadata = new DefaultFunctionMetadata
            {
                Name = "TestFunction",
                RawBindings = new List<string> { rawBinding },
            };

            RouteEndpoint endpoint = FunctionsEndpointDataSource.MapHttpFunction(metadata, routePrefix) as RouteEndpoint;

            Assert.Equal("TestFunction", endpoint.DisplayName);
            Assert.Equal($"{routePrefix}/TestFunction", endpoint.RoutePattern.RawText);
            var endpointMetadata = endpoint.Metadata.OfType<HttpMethodMetadata>().Single();
            Assert.Equal(new[] { "GET", "POST" }, endpointMetadata.HttpMethods);
        }

        [Fact]
        public void MapHttpFunction_CustomRoute()
        {
            string rawBinding = """
            {
                "name": "req",
                "direction": "In",
                "type": "httpTrigger",
                "route": "customRoute/function",
                "authLevel": "Anonymous",
                "methods": ["get", "post"],
                "properties": { }
            }
            """;

            var metadata = new DefaultFunctionMetadata
            {
                Name = "TestFunction",
                RawBindings = new List<string> { rawBinding },
            };

            RouteEndpoint endpoint = FunctionsEndpointDataSource.MapHttpFunction(metadata, "api") as RouteEndpoint;

            Assert.Equal("TestFunction", endpoint.DisplayName);
            Assert.Equal($"api/customRoute/function", endpoint.RoutePattern.RawText);
            var endpointMetadata = endpoint.Metadata.OfType<HttpMethodMetadata>().Single();
            Assert.Equal(new[] { "GET", "POST" }, endpointMetadata.HttpMethods);
        }

        [Fact]
        public void MapHttpFunction_CustomRoute_CaseInsensitive()
        {
            string rawBinding = """
            {
                "name": "req",
                "direction": "In",
                "tyPe": "httpTrigger",
                "rOute": "customRoute/function",
                "authLevel": "Anonymous",
                "metHOds": ["get", "post"],
                "properties": { }
            }
            """;

            var metadata = new DefaultFunctionMetadata
            {
                Name = "TestFunction",
                RawBindings = new List<string> { rawBinding },
            };

            RouteEndpoint endpoint = FunctionsEndpointDataSource.MapHttpFunction(metadata, "api") as RouteEndpoint;

            Assert.Equal("TestFunction", endpoint.DisplayName);
            Assert.Equal($"api/customRoute/function", endpoint.RoutePattern.RawText);
            var endpointMetadata = endpoint.Metadata.OfType<HttpMethodMetadata>().Single();
            Assert.Equal(new[] { "GET", "POST" }, endpointMetadata.HttpMethods);
        }

        [Fact]
        public void MapHttpFunction_CustomRoute_NoHttpMethodSpecified()
        {
            string rawBinding = """
            {
                "name": "req",
                "direction": "In",
                "type": "httpTrigger",
                "route": "customRoute/function",
                "authLevel": "Anonymous",
                "properties": { }
            }
            """;

            var metadata = new DefaultFunctionMetadata
            {
                Name = "TestFunction",
                RawBindings = new List<string> { rawBinding },
            };

            RouteEndpoint endpoint = FunctionsEndpointDataSource.MapHttpFunction(metadata, "api") as RouteEndpoint;

            Assert.Equal("TestFunction", endpoint.DisplayName);
            Assert.Equal($"api/customRoute/function", endpoint.RoutePattern.RawText);
            var endpointMetadata = endpoint.Metadata.OfType<HttpMethodMetadata>().Single();
            Assert.Equal([], endpointMetadata.HttpMethods);
        }

        [Fact]
        public void GetRoutePrefix()
        {
            string hostJson = """
            {
                "version": "2.0",
                "extensions": {  
                    "http": {
                        "routePrefix": "custom"
                    }
                }
            }
            """;

            string prefix = FunctionsEndpointDataSource.GetRoutePrefix(hostJson);
            Assert.Equal("custom", prefix);
        }

        [Fact]
        public void GetRoutePrefix_CaseInsensitive()
        {
            string hostJson = """
            {
                "version": "2.0",
                "ExTEnsions": {  
                    "hTtp": {
                        "rOUtepREfix": "custom"
                    }
                }
            }
            """;

            string prefix = FunctionsEndpointDataSource.GetRoutePrefix(hostJson);
            Assert.Equal("custom", prefix);
        }
    }
}
