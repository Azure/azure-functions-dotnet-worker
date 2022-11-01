// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;
using Google.Protobuf;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using static System.Net.Mime.MediaTypeNames;

namespace Microsoft.Azure.Functions.Worker.Grpc.NativeHostIntegration
{
    internal class NativeWorkerClient : IWorkerClient
    {
        private readonly IMessageProcessor _messageProcessor;
        private readonly ChannelReader<StreamingMessage> _outputChannelReader;
        private readonly ChannelWriter<StreamingMessage> _outputChannelWriter;
        private readonly NativeSafeHandle _application;
        private GCHandle _gcHandle;

        private readonly Channel<StreamingMessage> _inbound = Channel.CreateUnbounded<StreamingMessage>();

        public NativeWorkerClient(IMessageProcessor messageProcessor, GrpcHostChannel outputChannel, NativeHost nativeHostData)
        {
            _messageProcessor = messageProcessor;
            _outputChannelReader = outputChannel.Channel.Reader;
            _outputChannelWriter = outputChannel.Channel.Writer;
            _application = new NativeSafeHandle(nativeHostData.pNativeApplication);
        }

        public unsafe void Start()
        {
            _gcHandle = GCHandle.Alloc(this);
            NativeMethods.RegisterCallbacks(_application, &HandleRequest, (IntPtr)_gcHandle);

            _ = ProcessInbound();
            _ = ProcessOutbound();
        }

        private async Task ProcessInbound()
        {
            await foreach (StreamingMessage msg in _inbound.Reader.ReadAllAsync())
            {
                await _messageProcessor.ProcessMessageAsync(msg);
            }
        }

        private async Task ProcessOutbound()
        {
            await foreach (StreamingMessage msg in _outputChannelReader.ReadAllAsync())
            {
                NativeMethods.SendStreamingMessage(_application, msg);
            }
        }

        public ValueTask SendMessageAsync(StreamingMessage message)
        {
            return _outputChannelWriter.WriteAsync(message);
        }
        
        [UnmanagedCallersOnly]
        private static unsafe IntPtr HandleRequest(byte** nativeMessage, int nativeMessageSize, IntPtr grpcHandler)
        {
            var span = new ReadOnlySpan<byte>(*nativeMessage, nativeMessageSize);
            var msg = StreamingMessage.Parser.ParseFrom(span);

            NativeWorkerClient handler = (NativeWorkerClient)GCHandle.FromIntPtr(grpcHandler).Target!;
            handler._inbound.Writer.TryWrite(msg);

            return IntPtr.Zero;
        }
    }
}
