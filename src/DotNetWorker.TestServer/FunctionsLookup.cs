using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Grpc.FunctionMetadata;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Invocation;
using Microsoft.Azure.Functions.Worker.Sdk;

namespace Microsoft.Azure.Functions.Worker.TestServer;

internal class FunctionsLookup
{
    private event EventHandler<FunctionLoadedEventArgs>? FunctionLoaded;
    private class FunctionLoadedEventArgs : EventArgs
    {
    }

    private readonly IReadOnlyDictionary<string, TestFunctionDefinition> _functionsByName;
    private readonly IDictionary<string, bool> _functionsLoaded;
    private static readonly JsonSerializerOptions _serializerOptions = CreateSerializerOptions();

    public FunctionsLookup()
    {
        All = GetFunctionsMetadata();
        _functionsByName = All.ToDictionary(a => a.Name, a => Map(a, new DefaultMethodInfoLocator()));
        _functionsLoaded = All.ToDictionary(a => a.FunctionId, _ => false);
    }

    public IReadOnlyCollection<RpcFunctionMetadata> All { get; }

    public Task WaitForFunctionsLoading()
    {
        if (_functionsLoaded.All(a => a.Value))
            return Task.CompletedTask;
        var result = new TaskCompletionSource();
        FunctionLoaded += (_, _) =>
        {
            if (_functionsLoaded.All(a => a.Value))
                result.SetResult();
        };
        return result.Task;
    }


    private IReadOnlyCollection<RpcFunctionMetadata> GetFunctionsMetadata()
    {
        var generator = new FunctionMetadataGenerator();
        var functions = generator.GenerateFunctionMetadata(Assembly.GetExecutingAssembly().Location, Array.Empty<string>());
        return functions.Select(Map).ToArray();
    }

    private static RpcFunctionMetadata Map(SdkFunctionMetadata metadata)
    {
        var rpc = new RpcFunctionMetadata
        {
            EntryPoint = metadata.EntryPoint,
            FunctionId = Guid.NewGuid().ToString(),
            Name = metadata.Name,
            ScriptFile = metadata.ScriptFile
        };
        foreach (var property in metadata.Properties)
        {
            rpc.Properties.Add(property.Key, property.Value.ToString());
        }

        foreach (var bindingExpando in metadata.Bindings)
        {
            var bindingJson = JsonSerializer.Serialize(bindingExpando, _serializerOptions);
            rpc.RawBindings.Add(bindingJson);

            var binding = JsonSerializer.Deserialize<JsonElement>(bindingJson);
            if(!binding.TryGetProperty("name", out JsonElement jsonName)) throw new NullReferenceException("Missing property [name] on the binding");
            rpc.Bindings.Add(jsonName.ToString()!, FunctionMetadataRpcExtensions.CreateBindingInfo(binding));
        }

        return rpc;
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var namingPolicy = new FunctionsJsonNamingPolicy();

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true,
            IgnoreReadOnlyProperties = true,
            DictionaryKeyPolicy = namingPolicy,
            PropertyNamingPolicy = namingPolicy
        };

        options.Converters.Add(new JsonStringEnumConverter());

        return options;
    }

    public void IsLoaded(string functionId)
    {
        if (!_functionsLoaded.TryGetValue(functionId, out var isLoaded))
        {
            throw new ArgumentOutOfRangeException(nameof(functionId), $"The FunctionId {functionId} is not registered");
        }

        if (isLoaded)
        {
            return;
        }

        _functionsLoaded[functionId] = true;
        FunctionLoaded?.Invoke(this, new FunctionLoadedEventArgs());
    }

    public TestFunctionDefinition Lookup(string name)
    {
        return _functionsByName.TryGetValue(name, out var metadata) ? metadata : throw new ArgumentOutOfRangeException(nameof(name), $"The Function with name {name} is not registered");
    }

    private TestFunctionDefinition Map(RpcFunctionMetadata metadata, IMethodInfoLocator methodInfoLocator)
    {
        var entryPoint = metadata.EntryPoint;
        var name = metadata.Name;
        var id = metadata.FunctionId;

        string? scriptRoot = Environment.GetEnvironmentVariable("AzureWebJobsScriptRoot");
        if (string.IsNullOrWhiteSpace(scriptRoot))
        {
            throw new InvalidOperationException("The 'AzureWebJobsScriptRoot' environment variable value is not defined. This is a required environment variable that is automatically set by the Azure Functions runtime.");
        }

        if (string.IsNullOrWhiteSpace(metadata.ScriptFile))
        {
            throw new InvalidOperationException($"Metadata for function '{metadata.Name} ({metadata.FunctionId})' does not specify a 'ScriptFile'.");
        }
        string scriptFile = Path.Combine(scriptRoot, metadata.ScriptFile);
        var pathToAssembly = Path.GetFullPath(scriptFile);

        var raw = metadata.RawBindings.Select(a => JsonSerializer.Deserialize<JsonElement>(a)).ToDictionary(a => a.GetProperty("name").GetString()!);
        var grpcBindingsGroup = metadata.Bindings.GroupBy(kv => kv.Value.Direction);
        var grpcInputBindings = grpcBindingsGroup.FirstOrDefault(kv => kv.Key == BindingInfo.Types.Direction.In);
        var grpcOutputBindings = grpcBindingsGroup.FirstOrDefault(kv => kv.Key != BindingInfo.Types.Direction.In);
        var infoToMetadataLambda = new Func<KeyValuePair<string, BindingInfo>, BindingMetadata>(kv => new TestFunctionMetadata(kv.Key, kv.Value.Type, kv.Value.Direction == BindingInfo.Types.Direction.In ? BindingDirection.In : BindingDirection.Out, raw[kv.Key]));

        var inputBindings = grpcInputBindings?.ToImmutableDictionary(kv => kv.Key, infoToMetadataLambda)
               ?? ImmutableDictionary<string, BindingMetadata>.Empty;

        var outputBindings = grpcOutputBindings?.ToImmutableDictionary(kv => kv.Key, infoToMetadataLambda)
               ?? ImmutableDictionary<string, BindingMetadata>.Empty;

        var parameters = methodInfoLocator.GetMethod(pathToAssembly, entryPoint)
              .GetParameters()
              .Where(p => p.Name != null)
              .Select(p => new FunctionParameter(p.Name!, p.ParameterType, GetAdditionalPropertiesDictionary(p)))
              .ToImmutableArray();

        return new TestFunctionDefinition(name, id, entryPoint, pathToAssembly, inputBindings, outputBindings,
            parameters);
    }

    private ImmutableDictionary<string, object> GetAdditionalPropertiesDictionary(ParameterInfo parameterInfo)
    {
        // Get the input converter attribute information, if present on the parameter.
        var inputConverterAttribute = parameterInfo?.GetCustomAttribute<InputConverterAttribute>();

        if (inputConverterAttribute != null)
        {
            return new Dictionary<string, object>()
            {
                { "converterType", inputConverterAttribute.ConverterType.AssemblyQualifiedName! }
            }.ToImmutableDictionary();
        }

        return ImmutableDictionary<string, object>.Empty;
    }

    internal class TestFunctionDefinition : FunctionDefinition
    {
        /// <inheritdoc />
        public TestFunctionDefinition(string name, string id, string entryPoint, string pathToAssembly, IImmutableDictionary<string, BindingMetadata> inputBindings, IImmutableDictionary<string, BindingMetadata> outputBindings, ImmutableArray<FunctionParameter> parameters)
        {
            Name = name;
            Id = id;
            EntryPoint = entryPoint;
            PathToAssembly = pathToAssembly;
            InputBindings = inputBindings;
            OutputBindings = outputBindings;
            Parameters = parameters;
        }

        /// <inheritdoc />
        public override string Name { get; }

        /// <inheritdoc />
        public override string Id { get; }

        /// <inheritdoc />
        public override string EntryPoint { get; }

        /// <inheritdoc />
        public override string PathToAssembly { get; }

        /// <inheritdoc />
        public override IImmutableDictionary<string, BindingMetadata> InputBindings { get; }

        /// <inheritdoc />
        public override IImmutableDictionary<string, BindingMetadata> OutputBindings { get; }
        /// <inheritdoc />
        public override ImmutableArray<FunctionParameter> Parameters { get; }
    }

    internal class TestFunctionMetadata : BindingMetadata
    {
        /// <inheritdoc />
        public override string Name { get; }

        /// <inheritdoc />
        public override string Type { get; }

        /// <inheritdoc />
        public override BindingDirection Direction { get; }

        public JsonElement RawBinding { get; }


        /// <inheritdoc />
        public TestFunctionMetadata(string name, string type, BindingDirection direction, JsonElement rawBinding)
        {
            Name = name;
            Type = type;
            Direction = direction;
            RawBinding = rawBinding;
        }
    }
}
