// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Testing.Invocation;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Testing.Tests.Invocation;

public class FunctionInvocationTests
{
    [Fact]
    public async Task Invocation_ExecutesRealPipelineAndReturnsIdValueAndLogs()
    {
        await using var factory = CreateFactory();
        FunctionInvocationResult result = await factory.InvokeAsync(
            "GenericEcho",
            FunctionInvocationRequest.Create()
                .WithInput("request", FunctionTestValue.String("hello"))
                .WithInvocationId("invocation-public"));

        Assert.Equal("invocation-public", result.InvocationId);
        Assert.True(
            result.Status == FunctionInvocationStatus.Succeeded,
            $"Expected success but received {result.Status}: {result.Exception}");
        Assert.Equal("hello", Assert.IsType<FunctionTestValue.StringValue>(result.ReturnValue).Value);
        Assert.Contains(result.Logs, log => log.Message.Contains("GenericEcho received", StringComparison.Ordinal));
        result.EnsureSucceeded();
    }

    [Fact]
    public async Task Invocation_UserFailureReturnsStructuredFailedResult()
    {
        await using var factory = CreateFactory();
        FunctionInvocationResult result = await factory.InvokeAsync(
            "GenericFail",
            FunctionInvocationRequest.Create()
                .WithInput("request", FunctionTestValue.String("bad")));

        Assert.Equal(FunctionInvocationStatus.Failed, result.Status);
        Assert.NotNull(result.Exception);
        Assert.True(
            result.Exception.Message.Contains("Rejected 'bad'", StringComparison.Ordinal),
            result.Exception.ToString());
        Assert.Throws<InvalidOperationException>(result.EnsureSucceeded);
    }

    [Fact]
    public async Task Invocation_CallerCancellationReturnsCancelledResult()
    {
        await using var factory = CreateFactory();
        _ = factory.Services;
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        FunctionInvocationResult result = await factory.InvokeAsync(
            "GenericCancel",
            FunctionInvocationRequest.Create()
                .WithInput("request", FunctionTestValue.String("wait")),
            cancellation.Token);

        Assert.True(
            result.Status == FunctionInvocationStatus.Cancelled,
            $"Expected cancellation but received {result.Status}: {result.Exception}");
    }

    [Fact]
    public async Task Invocation_ProjectsEveryRetryExceptionFieldToFunctionCode()
    {
        await using var factory = CreateFactory();
        var previous = new FunctionInvocationException(
            "remote-source",
            "Remote.Exception",
            "remote-message",
            "remote-stack",
            true);

        FunctionInvocationResult result = await factory.InvokeAsync(
            "GenericRetry",
            FunctionInvocationRequest.Create()
                .WithInput("request", FunctionTestValue.String("retry"))
                .WithRetryContext(new FunctionTestRetryContext(2, 5, previous)));

        Assert.Equal(FunctionInvocationStatus.Succeeded, result.Status);
        Assert.Equal(
            "retry|2|5|remote-source|Remote.Exception|remote-message|remote-stack|True",
            Assert.IsType<FunctionTestValue.StringValue>(result.ReturnValue).Value);
    }

    [Fact]
    public async Task Invocation_MissingRetryExceptionIsNullInFunctionCode()
    {
        await using var factory = CreateFactory();

        FunctionInvocationResult result = await factory.InvokeAsync(
            "GenericRetry",
            FunctionInvocationRequest.Create()
                .WithInput("request", FunctionTestValue.String("first"))
                .WithRetryContext(new FunctionTestRetryContext(0, 5, null)));

        Assert.Equal(FunctionInvocationStatus.Succeeded, result.Status);
        Assert.Equal(
            "first|0|5|null|null|null|null|null",
            Assert.IsType<FunctionTestValue.StringValue>(result.ReturnValue).Value);
    }

    [Fact]
    public async Task Invocation_ProjectsTraceContextToFunctionCode()
    {
        await using var factory = CreateFactory();
        const string traceParent = "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01";
        const string traceState = "vendor=value";

        FunctionInvocationResult result = await factory.InvokeAsync(
            "GenericTrace",
            FunctionInvocationRequest.Create()
                .WithInput("request", FunctionTestValue.String("trace"))
                .WithTraceContext(new FunctionTestTraceContext(
                    traceParent,
                    traceState,
                    new Dictionary<string, string> { ["trace-key"] = "trace-value" },
                    new Dictionary<string, string> { ["baggage-key"] = "baggage-value" })));

        Assert.Equal(FunctionInvocationStatus.Succeeded, result.Status);
        Assert.Equal(
            $"trace|{traceParent}|{traceState}|trace-value|baggage-value",
            Assert.IsType<FunctionTestValue.StringValue>(result.ReturnValue).Value);
    }

    [Fact]
    public void Invocation_MapsOutputsAndTraceAttributes()
    {
        var response = new InvocationResponse
        {
            InvocationId = "output-invocation",
            Result = new StatusResult { Status = StatusResult.Types.Status.Success },
            ReturnValue = new TypedData { String = "return" }
        };
        response.OutputData.Add(new ParameterBinding
        {
            Name = "output",
            Data = new TypedData { Int = 42 }
        });
        response.TraceContextAttributes["trace-output"] = "captured";

        FunctionInvocationResult result = FunctionInvocationMapper.ToPublicResult(
            response,
            Array.Empty<RpcLog>());

        Assert.Equal("return", Assert.IsType<FunctionTestValue.StringValue>(result.ReturnValue).Value);
        Assert.Equal(42, Assert.IsType<FunctionTestValue.Int64Value>(result.Outputs["output"]).Value);
        Assert.Equal("captured", result.TraceAttributes["trace-output"]);
    }

    [Fact]
    public async Task Invocation_ConcurrentCallsRemainCorrelated()
    {
        await using var factory = CreateFactory();
        Task<FunctionInvocationResult>[] invocations = Enumerable.Range(0, 12)
            .Select(index => factory.InvokeAsync(
                "GenericEcho",
                FunctionInvocationRequest.Create()
                    .WithInput("request", FunctionTestValue.String($"value-{index}"))
                    .WithInvocationId($"concurrent-{index}")))
            .ToArray();

        FunctionInvocationResult[] results = await Task.WhenAll(invocations);

        Assert.Equal(12, results.Select(result => result.InvocationId).Distinct().Count());
        for (int index = 0; index < results.Length; index++)
        {
            Assert.Equal(FunctionInvocationStatus.Succeeded, results[index].Status);
            Assert.Equal(
                $"value-{index}",
                Assert.IsType<FunctionTestValue.StringValue>(results[index].ReturnValue).Value);
        }
    }

    [Fact]
    public void Invocation_BindingDataAliasPopulatesTriggerMetadata()
    {
        FunctionInvocationRequest request = FunctionInvocationRequest.Create()
            .WithBindingData("DequeueCount", FunctionTestValue.Int64(2));

        InvocationRequest rpc = FunctionInvocationMapper.ToRpcRequest("function", request, "invocation");

        Assert.Empty(rpc.InputData);
        Assert.Equal(2, rpc.TriggerMetadata["DequeueCount"].Int);
    }

    [Fact]
    public void Invocation_AllPublicValueCasesMapWithoutRpcLeakage()
    {
        FunctionTestModelBindingData model = new(
            "1.0",
            "tests",
            "application/octet-stream",
            new byte[] { 1, 2 });
        FunctionTestValue[] values =
        [
            FunctionTestValue.Null(),
            FunctionTestValue.String("text"),
            FunctionTestValue.Json("""{"value":1}"""),
            FunctionTestValue.Bytes(new byte[] { 1 }),
            FunctionTestValue.Int64(42),
            FunctionTestValue.Double(1.5),
            new FunctionTestValue.StringCollection(Array.AsReadOnly(new[] { "a", "b" })),
            new FunctionTestValue.BytesCollection(Array.AsReadOnly<ReadOnlyMemory<byte>>(
                [new byte[] { 1 }, new byte[] { 2 }])),
            new FunctionTestValue.Int64Collection(Array.AsReadOnly(new long[] { 1, 2 })),
            new FunctionTestValue.DoubleCollection(Array.AsReadOnly(new[] { 1.0, 2.0 })),
            new FunctionTestValue.ModelBindingValue(model),
            new FunctionTestValue.ModelBindingCollection(Array.AsReadOnly([model]))
        ];

        foreach (FunctionTestValue value in values)
        {
            TypedData rpc = FunctionInvocationMapper.ToRpcValue(value);
            FunctionTestValue roundTrip = FunctionInvocationMapper.ToPublicValue(rpc);
            Assert.Equal(value.GetType(), roundTrip.GetType());
        }

        IEnumerable<Type> rpcTypes = typeof(FunctionsApplicationFactory<>).Assembly
            .GetExportedTypes()
            .SelectMany(type => type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            .SelectMany(GetReferencedTypes)
            .Where(type => type.Namespace == "Microsoft.Azure.Functions.Worker.Grpc.Messages");
        Assert.Empty(rpcTypes);
    }

    [Fact]
    public async Task Invocation_StreamIsMaterializedBeforeDispatch()
    {
        await using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        FunctionTestValue value = await FunctionTestValue.FromStreamAsync(stream);

        Assert.Equal(new byte[] { 1, 2, 3 }, Assert.IsType<FunctionTestValue.BytesValue>(value).Value.ToArray());
    }

    [Fact]
    public void Invocation_InvalidModelsFailBeforeDispatch()
    {
        var duplicateNames = new Dictionary<string, FunctionTestValue>(StringComparer.Ordinal)
        {
            ["Value"] = FunctionTestValue.String("first"),
            ["value"] = FunctionTestValue.String("second")
        };
        Assert.Throws<ArgumentException>(() => new FunctionInvocationRequest(
            duplicateNames,
            new Dictionary<string, FunctionTestValue>()));

        FunctionInvocationRequest invalidTrace = FunctionInvocationRequest.Create()
            .WithTraceContext(new FunctionTestTraceContext(
                "invalid",
                string.Empty,
                new Dictionary<string, string>(),
                new Dictionary<string, string>()));
        Assert.Throws<ArgumentException>(() =>
            FunctionInvocationMapper.ToRpcRequest("function", invalidTrace, "invocation"));

        FunctionInvocationRequest invalidRetry = FunctionInvocationRequest.Create()
            .WithRetryContext(new FunctionTestRetryContext(1, 3, null));
        Assert.Throws<ArgumentException>(() =>
            FunctionInvocationMapper.ToRpcRequest("function", invalidRetry, "invocation"));

        var attributes = new Dictionary<string, string> { ["key"] = "before" };
        var trace = new FunctionTestTraceContext(
            "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
            string.Empty,
            attributes,
            new Dictionary<string, string>());
        attributes["key"] = "after";
        Assert.Equal("before", trace.Attributes["key"]);
    }

    [Fact]
    public async Task Invocation_UnknownFunctionFailsBeforeDispatch()
    {
        await using var factory = CreateFactory();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            factory.InvokeAsync("missing", FunctionInvocationRequest.Create()));
    }

    [Fact]
    public async Task Invocation_ThousandInvocationsRemainCorrelatedUnderBoundedConcurrency()
    {
        await using var factory = CreateFactory();
        const int invocationCount = 1_000;
        const int batchSize = 50;

        for (int offset = 0; offset < invocationCount; offset += batchSize)
        {
            Task<FunctionInvocationResult>[] batch = Enumerable.Range(offset, batchSize)
                .Select(index => factory.InvokeAsync(
                    "GenericEcho",
                    FunctionInvocationRequest.Create()
                        .WithInput("request", FunctionTestValue.String($"value-{index}"))
                        .WithInvocationId($"stress-{index}")))
                .ToArray();

            FunctionInvocationResult[] results = await Task.WhenAll(batch);
            for (int index = 0; index < results.Length; index++)
            {
                int expected = offset + index;
                Assert.Equal($"stress-{expected}", results[index].InvocationId);
                Assert.Equal(FunctionInvocationStatus.Succeeded, results[index].Status);
                Assert.Equal(
                    $"value-{expected}",
                    Assert.IsType<FunctionTestValue.StringValue>(results[index].ReturnValue).Value);
            }
        }
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

    private static IEnumerable<Type> GetReferencedTypes(MemberInfo member)
    {
        if (member is MethodInfo method)
        {
            yield return method.ReturnType;
            foreach (ParameterInfo parameter in method.GetParameters())
            {
                yield return parameter.ParameterType;
            }
        }
        else if (member is PropertyInfo property)
        {
            yield return property.PropertyType;
        }
        else if (member is FieldInfo field)
        {
            yield return field.FieldType;
        }
    }
}
