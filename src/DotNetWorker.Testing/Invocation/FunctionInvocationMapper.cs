// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using Google.Protobuf;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using RpcRetryContext = Microsoft.Azure.Functions.Worker.Grpc.Messages.RetryContext;

namespace Microsoft.Azure.Functions.Worker.Testing.Invocation;

internal static class FunctionInvocationMapper
{
    internal static InvocationRequest ToRpcRequest(
        string functionId,
        FunctionInvocationRequest request,
        string invocationId)
    {
        ValidateRequest(request, invocationId);
        var rpc = new InvocationRequest
        {
            FunctionId = functionId,
            InvocationId = invocationId,
            TraceContext = new RpcTraceContext()
        };

        foreach ((string name, FunctionTestValue value) in request.Inputs)
        {
            rpc.InputData.Add(new ParameterBinding { Name = name, Data = ToRpcValue(value) });
        }

        foreach ((string name, FunctionTestValue value) in request.TriggerMetadata)
        {
            rpc.TriggerMetadata.Add(name, ToRpcValue(value));
        }

        if (request.TraceContext is not null)
        {
            FunctionTestTraceContext trace = request.TraceContext;
            rpc.TraceContext = new RpcTraceContext
            {
                TraceParent = trace.TraceParent,
                TraceState = trace.TraceState ?? string.Empty
            };
            foreach ((string key, string value) in trace.Attributes)
            {
                rpc.TraceContext.Attributes.Add(key, value);
            }

            foreach ((string key, string value) in trace.Baggage)
            {
                rpc.TraceContext.Baggage.Add(key, value);
            }
        }

        if (request.RetryContext is not null)
        {
            FunctionTestRetryContext retry = request.RetryContext;
            rpc.RetryContext = new RpcRetryContext
            {
                RetryCount = retry.RetryCount,
                MaxRetryCount = retry.MaxRetryCount,
                Exception = ToRpcException(retry.PreviousException)
            };
        }

        return rpc;
    }

    internal static FunctionInvocationResult ToPublicResult(
        InvocationResponse response,
        IEnumerable<RpcLog> sessionLogs)
    {
        StatusResult.Types.Status rpcStatus = response.Result?.Status ?? StatusResult.Types.Status.Failure;
        FunctionInvocationStatus status = rpcStatus switch
        {
            StatusResult.Types.Status.Success => FunctionInvocationStatus.Succeeded,
            StatusResult.Types.Status.Cancelled => FunctionInvocationStatus.Cancelled,
            _ => FunctionInvocationStatus.Failed
        };
        if (status == FunctionInvocationStatus.Failed
            && response.Result?.Exception?.Type is "System.Threading.Tasks.TaskCanceledException"
                or "System.OperationCanceledException")
        {
            status = FunctionInvocationStatus.Cancelled;
        }

        var outputs = new Dictionary<string, FunctionTestValue>(StringComparer.OrdinalIgnoreCase);
        foreach (ParameterBinding output in response.OutputData)
        {
            if (!outputs.TryAdd(output.Name, ToPublicValue(output.Data)))
            {
                throw new InvalidOperationException($"The worker returned duplicate output name '{output.Name}' ignoring case.");
            }
        }

        IEnumerable<RpcLog> logs = sessionLogs
            .Where(log => string.Equals(log.InvocationId, response.InvocationId, StringComparison.Ordinal))
            .Concat(response.Result?.Logs ?? Enumerable.Empty<RpcLog>());

        return new FunctionInvocationResult(
            response.InvocationId,
            status,
            response.ReturnValue is null ? null : ToPublicValue(response.ReturnValue),
            new ReadOnlyDictionary<string, FunctionTestValue>(outputs),
            ToPublicException(response.Result?.Exception),
            Array.AsReadOnly(logs.Select(ToPublicLog).ToArray()),
            new ReadOnlyDictionary<string, string>(
                new Dictionary<string, string>(response.TraceContextAttributes, StringComparer.Ordinal)));
    }

    internal static TypedData ToRpcValue(FunctionTestValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value switch
        {
            FunctionTestValue.NullValue => new TypedData(),
            FunctionTestValue.StringValue item => new TypedData { String = item.Value ?? throw NullValue(nameof(item.Value)) },
            FunctionTestValue.JsonValue item => Json(item.Utf8Json),
            FunctionTestValue.BytesValue item => new TypedData { Bytes = ByteString.CopyFrom(item.Value.Span) },
            FunctionTestValue.Int64Value item => new TypedData { Int = item.Value },
            FunctionTestValue.DoubleValue item => new TypedData { Double = item.Value },
            FunctionTestValue.StringCollection item => Strings(item.Values),
            FunctionTestValue.BytesCollection item => Bytes(item.Values),
            FunctionTestValue.Int64Collection item => Integers(item.Values),
            FunctionTestValue.DoubleCollection item => Doubles(item.Values),
            FunctionTestValue.ModelBindingValue item => new TypedData { ModelBindingData = ModelBinding(item.Value) },
            FunctionTestValue.ModelBindingCollection item => ModelBindings(item.Values),
            _ => throw new NotSupportedException($"Function test value type '{value.GetType().FullName}' is not supported.")
        };
    }

    internal static FunctionTestValue ToPublicValue(TypedData value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.DataCase switch
        {
            TypedData.DataOneofCase.None => FunctionTestValue.Null(),
            TypedData.DataOneofCase.String => FunctionTestValue.String(value.String),
            TypedData.DataOneofCase.Json => FunctionTestValue.Json(value.Json),
            TypedData.DataOneofCase.Bytes => FunctionTestValue.Bytes(value.Bytes.Memory),
            TypedData.DataOneofCase.Stream => FunctionTestValue.Bytes(value.Stream.Memory),
            TypedData.DataOneofCase.Int => FunctionTestValue.Int64(value.Int),
            TypedData.DataOneofCase.Double => FunctionTestValue.Double(value.Double),
            TypedData.DataOneofCase.CollectionString => new FunctionTestValue.StringCollection(
                Array.AsReadOnly(value.CollectionString.String.ToArray())),
            TypedData.DataOneofCase.CollectionBytes => new FunctionTestValue.BytesCollection(
                Array.AsReadOnly(value.CollectionBytes.Bytes.Select(item => item.Memory).ToArray())),
            TypedData.DataOneofCase.CollectionSint64 => new FunctionTestValue.Int64Collection(
                Array.AsReadOnly(value.CollectionSint64.Sint64.ToArray())),
            TypedData.DataOneofCase.CollectionDouble => new FunctionTestValue.DoubleCollection(
                Array.AsReadOnly(value.CollectionDouble.Double.ToArray())),
            TypedData.DataOneofCase.ModelBindingData => new FunctionTestValue.ModelBindingValue(
                ToPublicModelBinding(value.ModelBindingData)),
            TypedData.DataOneofCase.CollectionModelBindingData => new FunctionTestValue.ModelBindingCollection(
                Array.AsReadOnly(value.CollectionModelBindingData.ModelBindingData.Select(ToPublicModelBinding).ToArray())),
            TypedData.DataOneofCase.Http => throw new NotSupportedException(
                "HTTP protocol values are available through CreateHttpClient(functionName), not FunctionTestValue."),
            _ => throw new NotSupportedException($"Worker value kind '{value.DataCase}' is not supported.")
        };
    }

    private static void ValidateRequest(FunctionInvocationRequest request, string invocationId)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(invocationId))
        {
            throw new ArgumentException("An invocation ID is required.", nameof(invocationId));
        }

        if (request.TraceContext is FunctionTestTraceContext trace)
        {
            if (string.IsNullOrWhiteSpace(trace.TraceParent)
                || !ActivityContext.TryParse(trace.TraceParent, trace.TraceState, out _))
            {
                throw new ArgumentException("TraceParent must be a valid W3C trace-parent value.", nameof(request));
            }

            ArgumentNullException.ThrowIfNull(trace.Attributes);
            ArgumentNullException.ThrowIfNull(trace.Baggage);
        }

        if (request.RetryContext is FunctionTestRetryContext retry)
        {
            if (retry.RetryCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(request), "RetryCount cannot be negative.");
            }

            if (retry.MaxRetryCount != -1 && retry.MaxRetryCount < retry.RetryCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(request),
                    "MaxRetryCount must be -1 or greater than or equal to RetryCount.");
            }

            if (retry.RetryCount > 0 && retry.PreviousException is null)
            {
                throw new ArgumentException("A previous exception is required when RetryCount is greater than zero.", nameof(request));
            }
        }
    }

    private static TypedData Json(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        using JsonDocument _ = JsonDocument.Parse(json);
        return new TypedData { Json = json };
    }

    private static TypedData Strings(IReadOnlyList<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var collection = new CollectionString();
        foreach (string value in values)
        {
            collection.String.Add(value ?? throw NullValue(nameof(values)));
        }

        return new TypedData { CollectionString = collection };
    }

    private static TypedData Bytes(IReadOnlyList<ReadOnlyMemory<byte>> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var collection = new CollectionBytes();
        collection.Bytes.Add(values.Select(value => ByteString.CopyFrom(value.Span)));
        return new TypedData { CollectionBytes = collection };
    }

    private static TypedData Integers(IReadOnlyList<long> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var collection = new CollectionSInt64();
        collection.Sint64.Add(values);
        return new TypedData { CollectionSint64 = collection };
    }

    private static TypedData Doubles(IReadOnlyList<double> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var collection = new CollectionDouble();
        collection.Double.Add(values);
        return new TypedData { CollectionDouble = collection };
    }

    private static TypedData ModelBindings(IReadOnlyList<FunctionTestModelBindingData> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var collection = new CollectionModelBindingData();
        collection.ModelBindingData.Add(values.Select(ModelBinding));
        return new TypedData { CollectionModelBindingData = collection };
    }

    private static ModelBindingData ModelBinding(FunctionTestModelBindingData value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (string.IsNullOrWhiteSpace(value.Version)
            || string.IsNullOrWhiteSpace(value.Source)
            || string.IsNullOrWhiteSpace(value.ContentType))
        {
            throw new ArgumentException(
                "Model-binding Version, Source, and ContentType must be non-empty.",
                nameof(value));
        }

        return new ModelBindingData
        {
            Version = value.Version,
            Source = value.Source,
            ContentType = value.ContentType,
            Content = ByteString.CopyFrom(value.Content.Span)
        };
    }

    private static FunctionTestModelBindingData ToPublicModelBinding(ModelBindingData value)
        => new(value.Version, value.Source, value.ContentType, value.Content.Memory);

    private static RpcException? ToRpcException(FunctionInvocationException? exception)
        => exception is null
            ? null
            : new RpcException
            {
                Source = exception.Source ?? string.Empty,
                Type = exception.Type ?? string.Empty,
                Message = exception.Message ?? string.Empty,
                StackTrace = exception.StackTrace ?? string.Empty,
                IsUserException = exception.IsUserException
            };

    private static FunctionInvocationException? ToPublicException(RpcException? exception)
        => exception is null
            ? null
            : new FunctionInvocationException(
                exception.Source,
                exception.Type,
                exception.Message,
                exception.StackTrace,
                exception.IsUserException);

    private static FunctionTestLogEntry ToPublicLog(RpcLog log)
        => new(
            (FunctionTestLogLevel)(int)log.Level,
            (FunctionTestLogCategory)(int)log.LogCategory,
            log.Category,
            log.Message,
            log.EventId,
            log.Properties,
            ToPublicException(log.Exception));

    private static ArgumentNullException NullValue(string parameterName)
        => new(parameterName, "Collection and scalar value payloads cannot contain null.");
}
