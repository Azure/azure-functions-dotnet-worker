using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Routing;
using Microsoft.Azure.Functions.Tests;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.AspNetMiddleware;
using Moq;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Tests.AspNetCore
{
    [Collection("Application directory")]
    public class FunctionsEndpointDataSourceTests
    {
        [Fact]
        public void EndpointsUseConfiguredApplicationDirectoryForMetadataAndRoutePrefix()
        {
            string root = Path.Combine(Path.GetTempPath(), "endpoint-directory-" + Guid.NewGuid().ToString("N"));
            string baseDirectory = Path.Combine(root, "base");
            string environmentDirectory = Path.Combine(root, "configured");
            Directory.CreateDirectory(baseDirectory);
            Directory.CreateDirectory(environmentDirectory);
            try
            {
                File.WriteAllText(Path.Combine(baseDirectory, "host.json"), """{"extensions":{"http":{"routePrefix":"base"}}}""");
                File.WriteAllText(Path.Combine(environmentDirectory, "host.json"), """{"extensions":{"http":{"routePrefix":"configured"}}}""");
                using TestScopedAppContextBaseDirectory application = new(baseDirectory);
                using TestScopedEnvironmentVariable environment = new("FUNCTIONS_APPLICATION_DIRECTORY", environmentDirectory);
                var metadata = new Mock<IFunctionMetadataManager>(MockBehavior.Strict);
                metadata.Setup(value => value.GetFunctionMetadataAsync(environmentDirectory))
                    .ReturnsAsync(ImmutableArray.Create<IFunctionMetadata>(new DefaultFunctionMetadata
                    {
                        Name = "Test",
                        RawBindings = new[] { """{"type":"httpTrigger","methods":["get"]}""" }
                    }));
                var source = new FunctionsEndpointDataSource(metadata.Object);

                var endpoint = Assert.IsType<RouteEndpoint>(Assert.Single(source.Endpoints));

                Assert.Equal("configured/Test", endpoint.RoutePattern.RawText);
                metadata.VerifyAll();
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

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

    [CollectionDefinition("Application directory", DisableParallelization = true)]
    public class ApplicationDirectoryCollection
    {
    }
}
