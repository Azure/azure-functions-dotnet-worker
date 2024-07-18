// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using FunctionsNetHost.Shared;
using Microsoft.Azure.Functions.Worker;
using SysEnv = System.Environment;

internal class StartupHook
{
    private const string LogSubCategory = nameof(StartupHook);

    // FunctionsNetHost will signal this handle when it receives environment reload request.
    static readonly EventWaitHandle WaitHandle = new(
        initialState: false,
        mode: EventResetMode.ManualReset,
        name: Constants.NetHostWaitHandleName
    );

    public static void Initialize()
    {
        var jitTraceFilePath = SysEnv.GetEnvironmentVariable(EnvironmentVariables.PreJitFilePath);
        if (string.IsNullOrWhiteSpace(jitTraceFilePath))
        {
            throw new InvalidOperationException($"Environment variable `{EnvironmentVariables.PreJitFilePath}` was not set. This behavior is unexpected.");
        }

        Log($"Pre-jitting using '{jitTraceFilePath}'.");
        PreJitPrepare(jitTraceFilePath);

#if NET8_0
        // In .NET 8.0, the SetEntryAssembly method is not part of the public API surface, so it must be accessed using reflection.
        var method = typeof(Assembly).GetMethod("SetEntryAssembly", BindingFlags.Static | BindingFlags.Public)
                 ?? throw new MissingMethodException($"Method 'Assembly.SetEntryAssembly' not found using reflection");
#endif

        Log("Waiting for cold start request.");
        // Now, wait for the cold start request. FNH will signal this wait handle when specialization request arrives.
        WaitHandle.WaitOne();

        var entryAssemblyFromCustomerPayload = SysEnv.GetEnvironmentVariable(EnvironmentVariables.SpecializedEntryAssembly);
        if (string.IsNullOrWhiteSpace(entryAssemblyFromCustomerPayload))
        {
            throw new InvalidOperationException($"Environment variable {EnvironmentVariables.SpecializedEntryAssembly} was not set. This behavior is unexpected.");
        }

        Assembly specializedEntryAssembly = Assembly.LoadFrom(entryAssemblyFromCustomerPayload);
        try
        {
#if NET8_0
            method.Invoke(null, [specializedEntryAssembly]);
            Log($"Specialized entry assembly set:{specializedEntryAssembly.FullName} using Assembly.SetEntryAssembly (via reflection)");

#elif NET9_0_OR_GREATER
            Assembly.SetEntryAssembly(specializedEntryAssembly);
            Log($"Specialized entry assembly set: {specializedEntryAssembly.FullName} using Assembly.SetEntryAssembly");
#endif
        }
        catch (Exception ex)
        {
            Log($"Error when trying to set entry assembly.{ex}.NET version:{RuntimeInformation.FrameworkDescription}");
        }
    }

    private static void Log(string message)
    {
        var ts = DateTime.UtcNow.ToString(Constants.LogTimeStampFormat, CultureInfo.InvariantCulture);
        Console.WriteLine($"{Constants.DefaultLogPrefix}[{ts}] [{Constants.LogCategory}][{LogSubCategory}] {message}");
    }

    private static void PreJitPrepare(string jitTraceFile)
    {
        if (!File.Exists(jitTraceFile))
        {
            Log($"File '{jitTraceFile}' not found.");
            return;
        }

        JitTraceRuntime.Prepare(new FileInfo(jitTraceFile), out int successes, out int failures);
        Log($"Successful Prepares: {successes},Failed Prepares: {failures}");
        JitKnownTypes();
    }

    private static void JitKnownTypes()
    {
        Assembly entryAssembly = Assembly.GetEntryAssembly();
        string version = System.Diagnostics.FileVersionInfo.GetVersionInfo(entryAssembly.Location).FileVersion;

        var jsonPath = Path.Combine(Path.GetDirectoryName(entryAssembly.Location), Path.GetFileNameWithoutExtension(entryAssembly.Location) + ".deps.json");
        var json = File.ReadAllText(jsonPath);
        var doc = JsonDocument.Parse(json);
        bool isParsed = Enum.TryParse<HookTypes>("One", out var hookType);

        // These assemblies would be loaded by the worker if they were not used here.
        var claim = new System.Security.Claims.Claim("type", "value");
        var alc = System.Runtime.Loader.AssemblyLoadContext.Default;
        var cookie = new System.Net.Cookie("name", "value");
        var regex = new System.Text.RegularExpressions.Regex("pattern");
        var ex = new System.Diagnostics.Tracing.EventSourceException();
        var aList = new System.Collections.ArrayList();
        var items = System.Collections.Immutable.ImmutableList.Create<string>();
        var stack = new System.Collections.Concurrent.ConcurrentStack<string>();
        var channel = System.Threading.Channels.Channel.CreateUnbounded<string>();
        var defaultExp = System.Linq.Expressions.Expression.Default(typeof(int));
        var client = new System.Net.Http.HttpClient();
        var methodCount = System.Runtime.JitInfo.GetCompiledMethodCount();
        var mmf = System.IO.MemoryMappedFiles.MemoryMappedFile.CreateNew(null, 1);
        var xdoc = new System.Xml.Linq.XDocument();
        var jsSerializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(string));
        var process = new System.Diagnostics.Process();
        var ipEntry = new System.Net.IPHostEntry();
        var cancelArgs = new System.ComponentModel.CancelEventArgs();
        var barrierEx = new System.Threading.BarrierPostPhaseException();
        var nInfoEx = new System.Net.NetworkInformation.NetworkInformationException();
        var authEx = new System.Security.Cryptography.AuthenticationTagMismatchException();
        var taEx = new System.Threading.ThreadInterruptedException();

        // Same with ASP.NET Core types.
        var provider = new Microsoft.Extensions.Configuration.EnvironmentVariables.EnvironmentVariablesConfigurationProvider();
        var fileInfo = new Microsoft.Extensions.FileProviders.Physical.PhysicalFileInfo(new FileInfo(SysEnv.GetEnvironmentVariable(EnvironmentVariables.PreJitFilePath)));
        var result = new Microsoft.Extensions.Options.ValidateOptionsResult();
        var cmdSource = new Microsoft.Extensions.Configuration.CommandLine.CommandLineConfigurationSource();
        var flex = new Microsoft.Extensions.Configuration.FileLoadExceptionContext();
        var listenerName = Microsoft.Extensions.Diagnostics.Metrics.ConsoleMetrics.DebugListenerName;
        var mops = new Microsoft.Extensions.Diagnostics.Metrics.MetricsOptions();
        var hb = new Microsoft.Extensions.Hosting.HostBuilder();
        var loggerFactory = new Microsoft.Extensions.Logging.LoggerFactory();
        var factory = new Microsoft.Extensions.DependencyInjection.DefaultServiceProviderFactory();
    }

    private enum HookTypes
    {
        One,
        Two,
        Three,
        Four,
        Five
    }
}
