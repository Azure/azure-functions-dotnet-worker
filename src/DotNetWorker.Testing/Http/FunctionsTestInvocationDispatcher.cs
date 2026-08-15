// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Microsoft.Azure.Functions.Worker.Testing.Invocation;
using Microsoft.Azure.Functions.Worker.Testing.Protocol;

namespace Microsoft.Azure.Functions.Worker.Testing.Http;

internal sealed class FunctionsTestInvocationDispatcher : IFunctionsTestInvocationDispatcher
{
    private readonly InMemoryFunctionsHost _protocol;
    private readonly TimeSpan _invocationTimeout;

    internal FunctionsTestInvocationDispatcher(
        InMemoryFunctionsHost protocol,
        string applicationDirectory,
        TimeSpan invocationTimeout)
    {
        _protocol = protocol;
        ApplicationDirectory = applicationDirectory;
        _invocationTimeout = invocationTimeout;
    }

    public string ApplicationDirectory { get; }

    public async Task<FunctionInvocationResult> InvokeHttpAsync(
        string functionName,
        FunctionsTestHttpRequest request,
        string invocationId,
        CancellationToken cancellationToken)
    {
        RpcFunctionMetadata function = FindFunction(functionName);
        string triggerName = FindHttpTriggerName(function);
        var invocation = new InvocationRequest
        {
            FunctionId = function.FunctionId,
            InvocationId = invocationId,
            TraceContext = new RpcTraceContext()
        };
        invocation.InputData.Add(new ParameterBinding
        {
            Name = triggerName,
            Data = new TypedData { Http = ToRpcHttp(request) }
        });

        InvocationResponse response = await InvokeRpcAsync(invocation, cancellationToken);
        return FunctionInvocationMapper.ToPublicResult(response, _protocol.Logs);
    }

    private async Task<InvocationResponse> InvokeRpcAsync(
        InvocationRequest request,
        CancellationToken cancellationToken)
    {
        Task<InvocationResponse> invocation = _protocol.InvokeAsync(
            request,
            _invocationTimeout,
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
            await _protocol.CancelInvocationAsync(
                request.InvocationId,
                _invocationTimeout,
                CancellationToken.None);
        }

        return await invocation;
    }

    private RpcFunctionMetadata FindFunction(string functionName)
    {
        RpcFunctionMetadata? function = _protocol.FunctionMetadata.SingleOrDefault(
            item => string.Equals(item.Name, functionName, StringComparison.OrdinalIgnoreCase));
        return function
            ?? throw new InvalidOperationException($"Function '{functionName}' was not found in the loaded metadata.");
    }

    private static string FindHttpTriggerName(RpcFunctionMetadata function)
    {
        string[] triggerNames = function.Bindings
            .Where(binding => string.Equals(binding.Value.Type, "httpTrigger", StringComparison.OrdinalIgnoreCase))
            .Select(binding => binding.Key)
            .ToArray();
        if (triggerNames.Length != 1)
        {
            throw new InvalidOperationException(
                $"Function '{function.Name}' must declare exactly one httpTrigger for ASP.NET Core testing.");
        }

        return triggerNames[0];
    }

    private static RpcHttp ToRpcHttp(FunctionsTestHttpRequest request)
    {
        var rpc = new RpcHttp
        {
            Method = request.Method,
            Url = request.Url.AbsoluteUri,
            Body = new TypedData { Bytes = ByteString.CopyFrom(request.Body.Span) },
            RawBody = new TypedData { Bytes = ByteString.CopyFrom(request.Body.Span) }
        };
        foreach ((string name, string value) in request.Headers)
        {
            rpc.Headers[name] = value;
            rpc.NullableHeaders[name] = new NullableString { Value = value };
        }

        string query = request.Url.Query.TrimStart('?');
        foreach (string pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = pair.Split('=', 2);
            string name = Uri.UnescapeDataString(parts[0].Replace("+", " ", StringComparison.Ordinal));
            string value = parts.Length == 1
                ? string.Empty
                : Uri.UnescapeDataString(parts[1].Replace("+", " ", StringComparison.Ordinal));
            rpc.Query[name] = value;
            rpc.NullableQuery[name] = new NullableString { Value = value };
        }

        return rpc;
    }
}
