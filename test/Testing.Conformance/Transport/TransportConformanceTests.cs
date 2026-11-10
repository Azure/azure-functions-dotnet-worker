// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.Testing.Conformance.Transport;

public class TransportConformanceTests
{
    [Fact]
    public async Task LoopbackGrpc_CompletesOneInvocation()
    {
        FunctionInvocationRequest request = FunctionInvocationRequest.Create()
            .WithInput("request", FunctionTestValue.String("smoke"))
            .WithInvocationId("transport-smoke");

        TransportDifferentialRunner.TransportRun serialized =
            await TransportDifferentialRunner.RunSerializedAsync("GenericEcho", request);

        Assert.Equal(FunctionInvocationStatus.Succeeded, serialized.Result.Status);
    }

    public static IEnumerable<object[]> Fixtures()
    {
        yield return Fixture(
            "GenericEcho",
            FunctionInvocationRequest.Create()
                .WithInput("request", FunctionTestValue.String("hello"))
                .WithInvocationId("transport-success"));
        yield return Fixture(
            "GenericFail",
            FunctionInvocationRequest.Create()
                .WithInput("request", FunctionTestValue.String("bad"))
                .WithInvocationId("transport-failure"));
        yield return Fixture(
            "GenericRetry",
            FunctionInvocationRequest.Create()
                .WithInput("request", FunctionTestValue.String("retry"))
                .WithRetryContext(new FunctionTestRetryContext(
                    2,
                    5,
                    new FunctionInvocationException(
                        "remote-source",
                        "Remote.Exception",
                        "remote-message",
                        "remote-stack",
                        true)))
                .WithInvocationId("transport-retry"));
        yield return Fixture(
            "GenericTrace",
            FunctionInvocationRequest.Create()
                .WithInput("request", FunctionTestValue.String("trace"))
                .WithTraceContext(new FunctionTestTraceContext(
                    "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
                    "vendor=value",
                    new Dictionary<string, string> { ["trace-key"] = "trace-value" },
                    new Dictionary<string, string> { ["baggage-key"] = "baggage-value" }))
                .WithInvocationId("transport-trace"));
        yield return Fixture(
            "GenericOutput",
            FunctionInvocationRequest.Create()
                .WithInput("request", FunctionTestValue.String("payload"))
                .WithInvocationId("transport-output"));

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
            FunctionTestValue.Bytes(new byte[] { 1, 2, 3 }),
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

        for (int index = 0; index < values.Length; index++)
        {
            yield return Fixture(
                "GenericEcho",
                FunctionInvocationRequest.Create()
                    .WithInput("request", FunctionTestValue.String($"value-{index}"))
                    .WithBindingData("fixture", values[index])
                    .WithInvocationId($"transport-value-{index}"));
        }
    }

    [Theory]
    [MemberData(nameof(Fixtures))]
    public async Task InMemoryTransport_MatchesSerializedLoopbackGrpc(
        string functionName,
        FunctionInvocationRequest request)
    {
        TransportDifferentialRunner.TransportRun inMemory =
            await TransportDifferentialRunner.RunInMemoryAsync(functionName, request);
        TransportDifferentialRunner.TransportRun serialized =
            await TransportDifferentialRunner.RunSerializedAsync(functionName, request);

        TransportDifferentialRunner.AssertEquivalent(inMemory, serialized);
    }

    [Fact]
    public async Task Cancellation_MatchesSerializedLoopbackGrpc()
    {
        FunctionInvocationRequest request = FunctionInvocationRequest.Create()
            .WithInput("request", FunctionTestValue.String("wait"))
            .WithInvocationId("transport-cancel");

        TransportDifferentialRunner.TransportRun inMemory =
            await TransportDifferentialRunner.RunInMemoryAsync(
                "GenericCancel",
                request,
                cancelAfterStartup: TimeSpan.FromMilliseconds(100));

        TransportDifferentialRunner.TransportRun serialized =
            await TransportDifferentialRunner.RunSerializedAsync(
                "GenericCancel",
                request,
                cancelAfterStartup: TimeSpan.FromMilliseconds(100));

        TransportDifferentialRunner.AssertEquivalent(inMemory, serialized);
    }

    private static object[] Fixture(string functionName, FunctionInvocationRequest request)
        => [functionName, request];
}
