using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.AspNetMiddleware
{
    internal class FunctionsEndpointDataSource : EndpointDataSource
    {
        private const string FunctionsApplicationDirectoryKey = "FUNCTIONS_APPLICATION_DIRECTORY";
        private const string HostJsonFileName = "host.json";
        private const string DefaultRoutePrefix = "api";

        private readonly IFunctionMetadataManager _functionMetadataManager;
        private readonly object _lock = new();

        private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
        {
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };


        private List<Endpoint>? _endpoints;

        public FunctionsEndpointDataSource(IFunctionMetadataManager functionMetadataManager)
        {
            _functionMetadataManager = functionMetadataManager ?? throw new ArgumentNullException(nameof(functionMetadataManager));
        }

        public override IReadOnlyList<Endpoint> Endpoints
        {
            get
            {
                if (_endpoints is null)
                {
                    lock (_lock)
                    {
                        _endpoints ??= BuildEndpoints();
                    }
                }

                return _endpoints;
            }
        }

        private List<Endpoint> BuildEndpoints()
        {
            List<Endpoint> endpoints = new List<Endpoint>();

            string scriptRoot = Environment.GetEnvironmentVariable(FunctionsApplicationDirectoryKey) ??
                           throw new InvalidOperationException("Cannot determine script root directory.");

            var metadata = _functionMetadataManager.GetFunctionMetadataAsync(scriptRoot).GetAwaiter().GetResult();

            string routePrefix = GetRoutePrefixFromHostJson(scriptRoot) ?? DefaultRoutePrefix;

            foreach (var functionMetadata in metadata)
            {
                var endpoint = MapHttpFunction(functionMetadata, routePrefix);

                if (endpoint is not null)
                {
                    endpoints.Add(endpoint);
                }
            }

            return endpoints;
        }

        public override IChangeToken GetChangeToken() => NullChangeToken.Singleton;

        internal static Endpoint? MapHttpFunction(IFunctionMetadata functionMetadata, string routePrefix)
        {
            if (functionMetadata.RawBindings is null)
            {
                return null;
            }

            var functionName = functionMetadata.Name ?? string.Empty;

            int order = 0;
            foreach (var binding in functionMetadata.RawBindings)
            {
                var functionBinding = JsonSerializer.Deserialize<FunctionHttpBinding>(binding, _jsonSerializerOptions);

                if (functionBinding is null)
                {
                    continue;
                }

                if (functionBinding.Type.Equals("httpTrigger", StringComparison.OrdinalIgnoreCase))
                {
                    string routeSuffix = functionBinding.Route ?? functionName;
                    string route = $"{routePrefix}/{routeSuffix}";

                    var pattern = RoutePatternFactory.Parse(route);

                    var endpointBuilder = new RouteEndpointBuilder(FunctionsHttpContextExtensions.InvokeFunctionAsync, pattern, order++)
                    {
                        DisplayName = functionName
                    };

                    var methods = functionBinding.Methods ?? [];
                    endpointBuilder.Metadata.Add(new HttpMethodMetadata(methods));

                    // no need to look at other bindings for this function
                    return endpointBuilder.Build();
                }
            }

            return null;
        }

        private static string? GetRoutePrefixFromHostJson(string scriptRoot)
        {
            string hostJsonPath = Path.Combine(scriptRoot, HostJsonFileName);

            if (!File.Exists(hostJsonPath))
            {
                return null;
            }

            string hostJsonString = File.ReadAllText(hostJsonPath);
            return GetRoutePrefix(hostJsonString);
        }

        internal static string? GetRoutePrefix(string hostJsonString)
        {
            var hostJson = JsonSerializer.Deserialize<HostJsonModel>(hostJsonString, _jsonSerializerOptions);
            return hostJson?.Extensions?.Http?.RoutePrefix;
        }
    }
}
