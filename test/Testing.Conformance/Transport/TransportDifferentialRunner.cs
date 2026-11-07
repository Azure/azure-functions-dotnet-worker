// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Google.Protobuf;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Testing.Protocol;

namespace Microsoft.Azure.Functions.Worker.Testing.Conformance.Transport;

internal static class TransportDifferentialRunner
{
    internal static async Task<TransportRun> RunInMemoryAsync(
        string functionName,
        FunctionInvocationRequest request,
        CancellationToken cancellationToken = default,
        TimeSpan? cancelAfterStartup = null)
    {
        InMemoryFunctionsHost? protocol = null;
        await using var factory = CreateFactory()
            .WithProtocolObserver(value => protocol = value);
        CancellationTokenSource? scheduledCancellation = null;
        try
        {
            if (cancelAfterStartup is TimeSpan delay)
            {
                _ = factory.Services;
                scheduledCancellation = new CancellationTokenSource(delay);
                cancellationToken = scheduledCancellation.Token;
            }

            FunctionInvocationResult result = await factory.InvokeAsync(functionName, request, cancellationToken)
                .WaitAsync(TimeSpan.FromSeconds(20));
            await factory.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(20));
            return new TransportRun(result, NormalizeTranscript(protocol!.Transcript));
        }
        finally
        {
            scheduledCancellation?.Dispose();
        }
    }

    internal static async Task<TransportRun> RunSerializedAsync(
        string functionName,
        FunctionInvocationRequest request,
        CancellationToken cancellationToken = default,
        TimeSpan? cancelAfterStartup = null)
    {
        await using var server = new LoopbackGrpcServer();
        InMemoryFunctionsHost? protocol = null;
        await using var factory = CreateFactory().WithSerializedGrpcTransport(
            server.Endpoint,
            value =>
            {
                protocol = value;
                server.AttachProtocol(value);
            });
        CancellationTokenSource? scheduledCancellation = null;
        try
        {
            if (cancelAfterStartup is TimeSpan delay)
            {
                _ = factory.Services;
                scheduledCancellation = new CancellationTokenSource(delay);
                cancellationToken = scheduledCancellation.Token;
            }

            FunctionInvocationResult result = await factory.InvokeAsync(functionName, request, cancellationToken)
                .WaitAsync(TimeSpan.FromSeconds(20));
            await factory.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(20));
            return new TransportRun(result, NormalizeTranscript(protocol!.Transcript));
        }
        finally
        {
            scheduledCancellation?.Dispose();
        }
    }

    internal static void AssertEquivalent(TransportRun expected, TransportRun actual)
    {
        Assert.Equal(NormalizeResult(expected.Result), NormalizeResult(actual.Result));
        Assert.Equal(expected.Transcript, actual.Transcript);
    }

    private static FunctionsApplicationFactory<ModernFunctionApp.Program> CreateFactory()
        => new FunctionsApplicationFactory<ModernFunctionApp.Program>()
            .WithContentRoot(GetFunctionOutput());

    private static string GetFunctionOutput()
        => Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "Resources",
            "Testing",
            "ModernFunctionApp",
            "bin",
            "Release",
            "net8.0"));

    private static string[] NormalizeTranscript(IReadOnlyList<ProtocolTranscriptEntry> transcript)
        => transcript
            .Where(static entry => entry.Message.ContentCase != StreamingMessage.ContentOneofCase.RpcLog)
            .Select(static entry =>
            {
                StreamingMessage message = entry.Message.Clone();
                message.RequestId = string.Empty;
                return $"{(entry.HostToWorker ? "host" : "worker")}:{message.ContentCase}:{Convert.ToBase64String(message.ToByteArray())}";
            })
            .ToArray();

    private static string NormalizeResult(FunctionInvocationResult result)
    {
        string exception = result.Exception is null
            ? string.Empty
            : $"{result.Exception.Type}|{result.Exception.Message}";
        string outputs = string.Join(
            ";",
            result.Outputs.OrderBy(static pair => pair.Key, StringComparer.Ordinal)
                .Select(static pair => $"{pair.Key}={NormalizeValue(pair.Value)}"));
        string traces = string.Join(
            ";",
            result.TraceAttributes.OrderBy(static pair => pair.Key, StringComparer.Ordinal)
                .Select(static pair => $"{pair.Key}={pair.Value}"));
        string logs = string.Join(
            ";",
            result.Logs.Select(static log => $"{log.Level}|{log.Category}|{log.Message}"));
        return string.Join(
            "\n",
            result.InvocationId,
            result.Status,
            exception,
            NormalizeValue(result.ReturnValue),
            outputs,
            traces,
            logs);
    }

    private static string NormalizeValue(FunctionTestValue? value)
        => value switch
        {
            null => "<missing>",
            FunctionTestValue.NullValue => "<null>",
            FunctionTestValue.StringValue item => $"string:{item.Value}",
            FunctionTestValue.JsonValue item => $"json:{item.Utf8Json}",
            FunctionTestValue.BytesValue item => $"bytes:{Convert.ToBase64String(item.Value.Span)}",
            FunctionTestValue.Int64Value item => $"int64:{item.Value}",
            FunctionTestValue.DoubleValue item => $"double:{item.Value:R}",
            FunctionTestValue.StringCollection item => $"strings:{string.Join(",", item.Values)}",
            FunctionTestValue.BytesCollection item => $"bytes[]:{string.Join(",", item.Values.Select(value => Convert.ToBase64String(value.Span)))}",
            FunctionTestValue.Int64Collection item => $"int64[]:{string.Join(",", item.Values)}",
            FunctionTestValue.DoubleCollection item => $"double[]:{string.Join(",", item.Values.Select(value => value.ToString("R")))}",
            FunctionTestValue.ModelBindingValue item => $"model:{NormalizeModel(item.Value)}",
            FunctionTestValue.ModelBindingCollection item => $"model[]:{string.Join(",", item.Values.Select(NormalizeModel))}",
            _ => throw new InvalidOperationException($"Unknown test value '{value.GetType()}'.")
        };

    private static string NormalizeModel(FunctionTestModelBindingData value)
        => $"{value.Version}|{value.Source}|{value.ContentType}|{Convert.ToBase64String(value.Content.Span)}";

    internal sealed record TransportRun(FunctionInvocationResult Result, IReadOnlyList<string> Transcript);
}
