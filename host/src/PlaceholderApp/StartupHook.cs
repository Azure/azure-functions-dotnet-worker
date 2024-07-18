using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using Microsoft.Azure.Functions.Worker;
using SysEnv = System.Environment;

internal class StartupHook
{
    // FNH will signal this handle when it receives env reload req.
    static readonly EventWaitHandle s_waitHandle = new EventWaitHandle(
        initialState: false,
        mode: EventResetMode.ManualReset,
        name: "AzureFunctionsNetHostSpecializationWaitHandle"
    );

    const string PrejitFileEnvVar        = "PREJIT_FILE_PATH";
    const string SpecEntryAssemblyEnvVar = "AZURE_FUNCTIONS_SPECIALIZED_ENTRY_ASSEMBLY";
    const string WorkerLogFileEnvVar     = "AZURE_FUNCTIONS_WORKER_LOGFILE_PATH";

    public static void Initialize()
    {
        LogMessage($"{SysEnv.NewLine}Startup Hook Called!{SysEnv.NewLine}");

        string jitTraceFile = SysEnv.GetEnvironmentVariable(PrejitFileEnvVar);
        
        if (!string.IsNullOrWhiteSpace(jitTraceFile))
        {
            LogMessage($"{PrejitFileEnvVar} env was set. Will attempt to carry out"
                       + $" the prejitting process using '{jitTraceFile}'.");
            PreJitPrepare(jitTraceFile);
        }

        s_waitHandle.WaitOne();

        string specEntryAsmName = SysEnv.GetEnvironmentVariable(SpecEntryAssemblyEnvVar);

        
       // string specEntryAsmName = SysEnv.GetEnvironmentVariable(SpecEntryAssemblyEnvVar);

        if (string.IsNullOrWhiteSpace(specEntryAsmName))
        {
            return ;
        }

        Assembly specializedEntryAsm = Assembly.LoadFrom(specEntryAsmName);
        Assembly.SetEntryAssembly(specializedEntryAsm);
        
    }

    private static void LogMessage(string message)
    {
        string logFile = SysEnv.GetEnvironmentVariable(WorkerLogFileEnvVar);

        if (string.IsNullOrWhiteSpace(logFile))
            Console.WriteLine($"STARTUP CONSOLE: {message}");
        else
            File.AppendAllTextAsync(logFile, $"{message}{SysEnv.NewLine}");
    }

    private static void PreJitPrepare(string jitTraceFile)
    {
        //LogMessage($"StartupHook.PreJitPrepare -> JitTraceFile: {jitTraceFile}");

        if (!File.Exists(jitTraceFile))
        {
            return ;
        }

        JitTraceRuntime.Prepare(new FileInfo(jitTraceFile),
                                out int successes,
                                out int failures);

        //LogMessage($"StartupHook.PreJitPrepare -> Successful Prepares: {successes}");
        //LogMessage($"StartupHook.PreJitPrepare -> Failed Prepares: {failures}");

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
        var fileInfo = new Microsoft.Extensions.FileProviders.Physical.PhysicalFileInfo(new FileInfo(SysEnv.GetEnvironmentVariable(PrejitFileEnvVar)));
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
