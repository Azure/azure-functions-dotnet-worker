// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Grpc;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Testing.Hosting;
using Microsoft.Azure.Functions.Worker.Testing.Http;
using Microsoft.Azure.Functions.Worker.Testing.Invocation;
using Microsoft.Azure.Functions.Worker.Testing.Protocol;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.Testing;

/// <summary>
/// Boots an Azure Functions .NET isolated worker application through its real entry point for integration testing.
/// </summary>
/// <typeparam name="TEntryPoint">A type from the executable function application assembly.</typeparam>
public class FunctionsApplicationFactory<TEntryPoint> : IDisposable, IAsyncDisposable
    where TEntryPoint : class
{
    private readonly IReadOnlyList<Action<IHostBuilder>> _hostConfigurations;
    private readonly IReadOnlyList<Action<IServiceCollection>> _serviceConfigurations;
    private readonly IReadOnlyDictionary<string, string?> _settings;
    private readonly FunctionsApplicationFactoryOptions _options;
    private readonly string? _contentRoot;
    private readonly IReadOnlySet<string> _httpCompanionActivations;
    private readonly Uri? _serializedGrpcEndpoint;
    private readonly Action<InMemoryFunctionsHost>? _serializedGrpcProtocolConfiguration;
    private readonly Lazy<Task<FactoryState>> _startup;
    private int _disposed;

    /// <summary>Initializes an unstarted factory using default options.</summary>
    public FunctionsApplicationFactory()
        : this(
            Array.Empty<Action<IHostBuilder>>(),
            Array.Empty<Action<IServiceCollection>>(),
            new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase),
            new FunctionsApplicationFactoryOptions(),
            contentRoot: null,
            new HashSet<string>(StringComparer.Ordinal),
            serializedGrpcEndpoint: null,
            serializedGrpcProtocolConfiguration: null)
    {
    }

    private FunctionsApplicationFactory(
        IReadOnlyList<Action<IHostBuilder>> hostConfigurations,
        IReadOnlyList<Action<IServiceCollection>> serviceConfigurations,
        IReadOnlyDictionary<string, string?> settings,
        FunctionsApplicationFactoryOptions options,
        string? contentRoot,
        IReadOnlySet<string> httpCompanionActivations,
        Uri? serializedGrpcEndpoint,
        Action<InMemoryFunctionsHost>? serializedGrpcProtocolConfiguration)
    {
        _hostConfigurations = hostConfigurations;
        _serviceConfigurations = serviceConfigurations;
        _settings = settings;
        _options = options;
        _contentRoot = contentRoot;
        _httpCompanionActivations = httpCompanionActivations;
        _serializedGrpcEndpoint = serializedGrpcEndpoint;
        _serializedGrpcProtocolConfiguration = serializedGrpcProtocolConfiguration;
        _startup = new Lazy<Task<FactoryState>>(StartAsync, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>Gets the started application's service provider.</summary>
    public IServiceProvider Services => GetState().Services;

    /// <summary>Gets the worker-only capability boundary for this factory.</summary>
    public FunctionsTestCapabilities Capabilities => FunctionsTestCapabilities.WorkerOnly;

    /// <summary>Invokes a named function through the worker's production invocation pipeline.</summary>
    public async Task<FunctionInvocationResult> InvokeAsync(
        string functionName,
        FunctionInvocationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        FactoryState state = await GetStateAsync(cancellationToken);
        RpcFunctionMetadata function = FindFunction(state.Protocol, functionName);
        string invocationId = string.IsNullOrWhiteSpace(request.InvocationId)
            ? Guid.NewGuid().ToString("N")
            : request.InvocationId;
        InvocationRequest rpcRequest = FunctionInvocationMapper.ToRpcRequest(
            function.FunctionId,
            request,
            invocationId);

        InvocationResponse response;
        try
        {
            response = await InvokeRpcAsync(state.Protocol, rpcRequest, cancellationToken);
        }
        catch (TimeoutException exception)
        {
            throw new FunctionsTestHostException(
                $"The in-memory worker transport timed out while invoking function '{functionName}'.",
                exception);
        }

        return FunctionInvocationMapper.ToPublicResult(response, state.Protocol.Logs);
    }

    /// <summary>
    /// Creates a function-targeted client for built-in
    /// <see cref="Microsoft.Azure.Functions.Worker.Http.HttpRequestData"/> functions.
    /// URL routing, authorization, and host-owned HTTP behavior are intentionally not simulated.
    /// </summary>
    public HttpClient CreateHttpClient(
        string functionName,
        FunctionsHttpClientOptions? options = null)
    {
        options ??= new FunctionsHttpClientOptions();
        options.Validate();
        FactoryState state = GetState();
        RpcFunctionMetadata function = FindFunction(state.Protocol, functionName);
        string triggerName = FindHttpTriggerName(function);
        var client = new HttpClient(new BuiltInHttpMessageHandler<TEntryPoint>(this, function.Name, triggerName))
        {
            BaseAddress = options.BaseAddress
        };
        return client;
    }

    /// <summary>Creates a client supplied by an explicitly activated HTTP companion.</summary>
    public HttpClient CreateClient(FunctionsTestClientOptions? options = null)
    {
        options ??= new FunctionsTestClientOptions();
        options.Validate();
        IFunctionsTestHttpClientProvider[] providers = Services
            .GetServices<IFunctionsTestHttpClientProvider>()
            .ToArray();
        if (providers.Length != 1)
        {
            throw new NotSupportedException(
                providers.Length == 0
                    ? "No worker HTTP companion is active. Use CreateHttpClient(functionName) for built-in "
                        + "HttpRequestData functions, or call WithAspNetCore() from the ASP.NET Core testing companion."
                    : "Multiple worker HTTP client providers are registered; exactly one companion provider is required.");
        }

        return new HttpClient(providers[0].CreateHandler(options))
        {
            BaseAddress = options.BaseAddress
        };
    }

    /// <summary>Returns an independent unstarted factory with an additional host-builder callback.</summary>
    public FunctionsApplicationFactory<TEntryPoint> WithHostBuilder(Action<IHostBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        EnsureCanConfigure();
        return Clone(hostConfigurations: Append(_hostConfigurations, configure));
    }

    /// <summary>Returns an independent unstarted factory with a last-wins service callback.</summary>
    public FunctionsApplicationFactory<TEntryPoint> WithServices(Action<IServiceCollection> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        EnsureCanConfigure();
        return Clone(serviceConfigurations: Append(_serviceConfigurations, configure));
    }

    /// <summary>Returns an independent unstarted factory with an in-memory configuration setting.</summary>
    public FunctionsApplicationFactory<TEntryPoint> WithSetting(string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("A non-empty configuration key is required.", nameof(key));
        }

        EnsureCanConfigure();
        var settings = new Dictionary<string, string?>(_settings, StringComparer.OrdinalIgnoreCase)
        {
            [key] = value
        };
        return Clone(settings: settings);
    }

    /// <summary>Returns an independent unstarted factory using an explicit function output directory.</summary>
    public FunctionsApplicationFactory<TEntryPoint> WithContentRoot(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("A content-root path is required.", nameof(path));
        }

        EnsureCanConfigure();
        string fullPath = Path.GetFullPath(path);
        if (!Path.IsPathFullyQualified(fullPath) || !Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException($"The function content root '{fullPath}' does not exist.");
        }

        return Clone(contentRoot: fullPath, replaceContentRoot: true);
    }

    /// <summary>Returns an independent unstarted factory with modified validated options.</summary>
    public FunctionsApplicationFactory<TEntryPoint> WithOptions(Action<FunctionsApplicationFactoryOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        EnsureCanConfigure();
        FunctionsApplicationFactoryOptions options = _options.Clone();
        configure(options);
        options.Validate();
        return Clone(options: options);
    }

    internal FunctionsApplicationFactory<TEntryPoint> WithHttpCompanion(
        string activationId,
        Action<IHostBuilder> configure)
    {
        if (string.IsNullOrWhiteSpace(activationId))
        {
            throw new ArgumentException("A companion activation ID is required.", nameof(activationId));
        }

        ArgumentNullException.ThrowIfNull(configure);
        EnsureCanConfigure();
        var activations = new HashSet<string>(_httpCompanionActivations, StringComparer.Ordinal);
        bool added = activations.Add(activationId);
        return Clone(
            hostConfigurations: added ? Append(_hostConfigurations, configure) : _hostConfigurations,
            httpCompanionActivations: activations);
    }

    internal FunctionsApplicationFactory<TEntryPoint> WithSerializedGrpcTransport(
        Uri endpoint,
        Action<InMemoryFunctionsHost> configureProtocol)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(configureProtocol);
        EnsureCanConfigure();
        if (!endpoint.IsAbsoluteUri || endpoint.Scheme != Uri.UriSchemeHttp)
        {
            throw new ArgumentException("The loopback gRPC endpoint must be an absolute HTTP URI.", nameof(endpoint));
        }

        var settings = new Dictionary<string, string?>(_settings, StringComparer.OrdinalIgnoreCase)
        {
            ["Functions:Worker:HostEndpoint"] = endpoint.AbsoluteUri,
            ["Functions:Worker:WorkerId"] = InMemoryFunctionsHost.TestWorkerId,
            ["Functions:Worker:RequestId"] = "testing-loopback-request",
            ["Functions:Worker:GrpcMaxMessageLength"] = _options.MaxMessageLength.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };
        return Clone(
            settings: settings,
            serializedGrpcEndpoint: endpoint,
            serializedGrpcProtocolConfiguration: configureProtocol);
    }

    internal FunctionsApplicationFactory<TEntryPoint> WithProtocolObserver(
        Action<InMemoryFunctionsHost> observeProtocol)
    {
        ArgumentNullException.ThrowIfNull(observeProtocol);
        EnsureCanConfigure();
        return Clone(serializedGrpcProtocolConfiguration: observeProtocol);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        if (!_startup.IsValueCreated)
        {
            return;
        }

        FactoryState state;
        try
        {
            state = await _startup.Value;
        }
        catch
        {
            return;
        }

        await state.Protocol.DisposeAsync();

        using var shutdown = new CancellationTokenSource(_options.ShutdownTimeout);
        try
        {
            await state.Host.StopAsync(shutdown.Token);
        }
        catch (OperationCanceledException) when (shutdown.IsCancellationRequested)
        {
        }

        if (state.Host is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else
        {
            state.Host.Dispose();
        }
    }

    private FactoryState GetState()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        return _startup.Value.GetAwaiter().GetResult();
    }

    private async Task<FactoryState> GetStateAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        return await _startup.Value.WaitAsync(cancellationToken);
    }

    internal async Task<InvocationResponse> InvokeHttpAsync(
        string functionName,
        string triggerName,
        RpcHttp http,
        CancellationToken cancellationToken)
    {
        FactoryState state = await GetStateAsync(cancellationToken);
        RpcFunctionMetadata function = FindFunction(state.Protocol, functionName);
        var request = new InvocationRequest
        {
            FunctionId = function.FunctionId,
            InvocationId = Guid.NewGuid().ToString("N"),
            TraceContext = new RpcTraceContext()
        };
        request.InputData.Add(new ParameterBinding
        {
            Name = triggerName,
            Data = new TypedData { Http = http }
        });
        return await InvokeRpcAsync(state.Protocol, request, cancellationToken);
    }

    private async Task<InvocationResponse> InvokeRpcAsync(
        InMemoryFunctionsHost protocol,
        InvocationRequest request,
        CancellationToken cancellationToken)
    {
        Task<InvocationResponse> invocation = protocol.InvokeAsync(
            request,
            _options.InvocationTimeout,
            CancellationToken.None);
        if (!cancellationToken.CanBeCanceled)
        {
            return await invocation;
        }

        var cancellation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using CancellationTokenRegistration registration = cancellationToken.UnsafeRegister(
            static state => ((TaskCompletionSource)state!).TrySetResult(),
            cancellation);

        if (await Task.WhenAny(invocation, cancellation.Task) != invocation)
        {
            await protocol.CancelInvocationAsync(
                request.InvocationId,
                _options.InvocationTimeout,
                CancellationToken.None);
        }

        return await invocation;
    }

    private async Task<FactoryState> StartAsync()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        _options.Validate();
        ValidatePackageCompatibility();

        Assembly applicationAssembly = typeof(TEntryPoint).Assembly;
        if (applicationAssembly.EntryPoint is null)
        {
            throw new InvalidOperationException(
                $"Assembly '{applicationAssembly.FullName}' has no executable entry point. TEntryPoint must come from the function application executable.");
        }

        string contentRoot = ResolveContentRoot(applicationAssembly);
        ValidateFunctionOutput(contentRoot);

        var protocol = new InMemoryFunctionsHost(_options.ShutdownTimeout, _options.MaxMessageLength);
        _serializedGrpcProtocolConfiguration?.Invoke(protocol);
        var builder = new DeferredFunctionsHostBuilder();
        builder.UseEnvironment(_options.EnvironmentName);
        builder.UseContentRoot(contentRoot);
        builder.ConfigureHostConfiguration(configuration =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [HostDefaults.ApplicationKey] = applicationAssembly.GetName().Name ?? string.Empty
            }));

        if (_settings.Count > 0)
        {
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(_settings));
        }

        foreach (Action<IHostBuilder> configure in _hostConfigurations)
        {
            configure(builder);
        }

        foreach (Action<IServiceCollection> configure in _serviceConfigurations)
        {
            builder.ConfigureServices((_, services) => configure(services));
        }

        builder.ConfigureServices((_, services) =>
        {
            RemoveUnsupportedEventLogProvider(services);
            services.AddSingleton(protocol);
            services.AddSingleton<IFunctionsTestInvocationDispatcher>(
                new FunctionsTestInvocationDispatcher(
                    protocol,
                    contentRoot,
                    _options.InvocationTimeout));
            if (_serializedGrpcEndpoint is null)
            {
                services.AddSingleton<InMemoryWorkerClientFactory>();
                services.Replace(ServiceDescriptor.Singleton<IWorkerClientFactory>(provider =>
                    provider.GetRequiredService<InMemoryWorkerClientFactory>()));
            }
        });

        Func<string[], object>? hostFactory = HostFactoryResolver.ResolveHostFactory(
            applicationAssembly,
            _options.StartupTimeout,
            builder.ConfigureHostBuilder,
            builder.EntryPointCompleted);
        if (hostFactory is null)
        {
            throw new InvalidOperationException(
                $"Assembly '{applicationAssembly.FullName}' does not expose an executable entry point that builds an IHost.");
        }

        builder.SetHostFactory(hostFactory);
        IHost host = builder.Build();

        using var startup = new CancellationTokenSource(_options.StartupTimeout);
        try
        {
            await host.StartAsync(startup.Token);
            await protocol.InitializeAsync(contentRoot, _options.StartupTimeout, startup.Token);
            return new FactoryState(host, protocol);
        }
        catch (Exception exception)
        {
            await protocol.DisposeAsync();
            if (host is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else
            {
                host.Dispose();
            }

            if (exception is OperationCanceledException && startup.IsCancellationRequested)
            {
                throw new TimeoutException(
                    $"The function application did not start within {_options.StartupTimeout}.",
                    exception);
            }

            throw;
        }
    }

    private string ResolveContentRoot(Assembly applicationAssembly)
    {
        if (_contentRoot is not null)
        {
            return _contentRoot;
        }

        string? location = Path.GetDirectoryName(applicationAssembly.Location);
        if (string.IsNullOrEmpty(location) || !Directory.Exists(location))
        {
            throw new DirectoryNotFoundException(
                $"Could not resolve a content root beside '{applicationAssembly.Location}'. Use WithContentRoot with the function build output directory.");
        }

        return Path.GetFullPath(location);
    }

    private static void ValidatePackageCompatibility()
    {
        Assembly testingAssembly = typeof(FunctionsApplicationFactory<TEntryPoint>).Assembly;
        AssemblyCompatibilityValidator.ValidateReferencedAssembly(testingAssembly, typeof(FunctionContext).Assembly);
        AssemblyCompatibilityValidator.ValidateReferencedAssembly(
            testingAssembly,
            typeof(Microsoft.Extensions.Hosting.WorkerHostBuilderExtensions).Assembly);
        AssemblyCompatibilityValidator.ValidateReferencedAssembly(testingAssembly, typeof(GrpcWorker).Assembly);
    }

    private static void ValidateFunctionOutput(string contentRoot)
    {
        foreach (string requiredFile in new[] { "host.json", "functions.metadata" })
        {
            string path = Path.Combine(contentRoot, requiredFile);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    $"The function build output '{contentRoot}' does not contain required file '{requiredFile}'. Use WithContentRoot for shadow-copy test layouts.",
                    path);
            }
        }
    }

    private static void RemoveUnsupportedEventLogProvider(IServiceCollection services)
    {
        for (int index = services.Count - 1; index >= 0; index--)
        {
            ServiceDescriptor descriptor = services[index];
            if (descriptor.ServiceType == typeof(ILoggerProvider)
                && descriptor.ImplementationType?.FullName
                    == "Microsoft.Extensions.Logging.EventLog.EventLogLoggerProvider")
            {
                services.RemoveAt(index);
            }
        }
    }

    private static RpcFunctionMetadata FindFunction(
        InMemoryFunctionsHost protocol,
        string functionName)
    {
        if (string.IsNullOrWhiteSpace(functionName))
        {
            throw new ArgumentException("A non-empty function name is required.", nameof(functionName));
        }

        RpcFunctionMetadata? function = protocol.FunctionMetadata.SingleOrDefault(
            item => string.Equals(item.Name, functionName, StringComparison.OrdinalIgnoreCase));
        return function
            ?? throw new InvalidOperationException($"Function '{functionName}' was not found in the loaded metadata.");
    }

    private static string FindHttpTriggerName(RpcFunctionMetadata function)
    {
        string[] triggerNames = function.RawBindings
            .Select(GetHttpTriggerName)
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .ToArray();

        if (triggerNames.Length != 1)
        {
            throw new InvalidOperationException(
                $"Function '{function.Name}' must declare exactly one httpTrigger to use CreateHttpClient(functionName).");
        }

        return triggerNames[0];
    }

    private static string? GetHttpTriggerName(string binding)
    {
        using JsonDocument document = JsonDocument.Parse(binding);
        return document.RootElement.TryGetProperty("type", out JsonElement type)
            && string.Equals(type.GetString(), "httpTrigger", StringComparison.OrdinalIgnoreCase)
            && document.RootElement.TryGetProperty("name", out JsonElement name)
                ? name.GetString()
                : null;
    }

    private void EnsureCanConfigure()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        if (_startup.IsValueCreated)
        {
            throw new InvalidOperationException("Factory configuration cannot be changed after startup has begun.");
        }
    }

    private FunctionsApplicationFactory<TEntryPoint> Clone(
        IReadOnlyList<Action<IHostBuilder>>? hostConfigurations = null,
        IReadOnlyList<Action<IServiceCollection>>? serviceConfigurations = null,
        IReadOnlyDictionary<string, string?>? settings = null,
        FunctionsApplicationFactoryOptions? options = null,
        string? contentRoot = null,
        bool replaceContentRoot = false,
        IReadOnlySet<string>? httpCompanionActivations = null,
        Uri? serializedGrpcEndpoint = null,
        Action<InMemoryFunctionsHost>? serializedGrpcProtocolConfiguration = null)
        => new(
            hostConfigurations ?? _hostConfigurations,
            serviceConfigurations ?? _serviceConfigurations,
            settings ?? _settings,
            options ?? _options.Clone(),
            replaceContentRoot ? contentRoot : _contentRoot,
            httpCompanionActivations ?? _httpCompanionActivations,
            serializedGrpcEndpoint ?? _serializedGrpcEndpoint,
            serializedGrpcProtocolConfiguration ?? _serializedGrpcProtocolConfiguration);

    private static IReadOnlyList<T> Append<T>(IReadOnlyList<T> source, T item)
    {
        var result = new T[source.Count + 1];
        for (int index = 0; index < source.Count; index++)
        {
            result[index] = source[index];
        }

        result[^1] = item;
        return result;
    }

    private sealed record FactoryState(IHost Host, InMemoryFunctionsHost Protocol)
    {
        internal IServiceProvider Services => Host.Services;
    }
}
