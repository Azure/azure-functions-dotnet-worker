// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Google.Protobuf;
using Grpc.Core;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Testing.Protocol;

namespace Microsoft.Azure.Functions.Worker.Testing.Conformance.Transport;

internal sealed class LoopbackGrpcServer : IAsyncDisposable
{
    private static readonly Marshaller<StreamingMessage> MessageMarshaller = Marshallers.Create(
        static message => message.ToByteArray(),
        static payload => StreamingMessage.Parser.ParseFrom(payload));

    private static readonly Method<StreamingMessage, StreamingMessage> EventStreamMethod = new(
        MethodType.DuplexStreaming,
        "AzureFunctionsRpcMessages.FunctionRpc",
        "EventStream",
        MessageMarshaller,
        MessageMarshaller);

    private readonly Server _server;
    private readonly TaskCompletionSource<InMemoryFunctionsHost> _protocol =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal LoopbackGrpcServer()
    {
        ServerServiceDefinition service = ServerServiceDefinition.CreateBuilder()
            .AddMethod(EventStreamMethod, HandleEventStreamAsync)
            .Build();
        _server = new Server
        {
            Services = { service },
            Ports = { new ServerPort("127.0.0.1", 0, ServerCredentials.Insecure) }
        };
        _server.Start();
        Endpoint = new Uri($"http://127.0.0.1:{_server.Ports.Single().BoundPort}");
    }

    internal Uri Endpoint { get; }

    internal void AttachProtocol(InMemoryFunctionsHost protocol)
        => _protocol.TrySetResult(protocol);

    public async ValueTask DisposeAsync()
        => await _server.KillAsync();

    private async Task HandleEventStreamAsync(
        IAsyncStreamReader<StreamingMessage> requests,
        IServerStreamWriter<StreamingMessage> responses,
        ServerCallContext context)
    {
        InMemoryFunctionsHost protocol = await _protocol.Task.WaitAsync(context.CancellationToken);
        if (!await requests.MoveNext(context.CancellationToken))
        {
            return;
        }

        await protocol.AcceptWorkerMessageAsync(requests.Current);
        protocol.Connect(message => responses.WriteAsync(message));

        while (await requests.MoveNext(context.CancellationToken))
        {
            await protocol.AcceptWorkerMessageAsync(requests.Current);
        }
    }
}
