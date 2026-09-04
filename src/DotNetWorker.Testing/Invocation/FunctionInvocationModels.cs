// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.Azure.Functions.Worker.Testing;

/// <summary>Describes a synthetic function invocation.</summary>
public sealed record FunctionInvocationRequest
{
    /// <summary>Initializes an immutable invocation request.</summary>
    public FunctionInvocationRequest(
        IReadOnlyDictionary<string, FunctionTestValue> inputs,
        IReadOnlyDictionary<string, FunctionTestValue> triggerMetadata,
        FunctionTestTraceContext? traceContext = null,
        FunctionTestRetryContext? retryContext = null,
        string? invocationId = null)
    {
        Inputs = CopyValues(inputs, nameof(inputs));
        TriggerMetadata = CopyValues(triggerMetadata, nameof(triggerMetadata));
        TraceContext = traceContext;
        RetryContext = retryContext;
        InvocationId = invocationId;
    }

    /// <summary>Gets input-binding values keyed by binding name.</summary>
    public IReadOnlyDictionary<string, FunctionTestValue> Inputs { get; init; }

    /// <summary>Gets trigger metadata keyed by metadata name.</summary>
    public IReadOnlyDictionary<string, FunctionTestValue> TriggerMetadata { get; init; }

    /// <summary>Gets the optional trace context.</summary>
    public FunctionTestTraceContext? TraceContext { get; init; }

    /// <summary>Gets the optional retry context.</summary>
    public FunctionTestRetryContext? RetryContext { get; init; }

    /// <summary>Gets an optional caller-supplied invocation identifier.</summary>
    public string? InvocationId { get; init; }

    /// <summary>Creates an empty invocation request.</summary>
    public static FunctionInvocationRequest Create()
        => new(
            new Dictionary<string, FunctionTestValue>(),
            new Dictionary<string, FunctionTestValue>());

    /// <summary>Returns a request with one input value added or replaced.</summary>
    public FunctionInvocationRequest WithInput(string name, FunctionTestValue value)
        => this with { Inputs = AddValue(Inputs, name, value) };

    /// <summary>Returns a request with one trigger-metadata value added or replaced.</summary>
    public FunctionInvocationRequest WithTriggerMetadata(string name, FunctionTestValue value)
        => this with { TriggerMetadata = AddValue(TriggerMetadata, name, value) };

    /// <summary>
    /// Returns a request with one binding-data value stored in trigger metadata.
    /// The worker protocol does not contain a third binding-data dictionary.
    /// </summary>
    public FunctionInvocationRequest WithBindingData(string name, FunctionTestValue value)
        => WithTriggerMetadata(name, value);

    /// <summary>Returns a request with trace context.</summary>
    public FunctionInvocationRequest WithTraceContext(FunctionTestTraceContext? traceContext)
        => this with { TraceContext = traceContext };

    /// <summary>Returns a request with retry context.</summary>
    public FunctionInvocationRequest WithRetryContext(FunctionTestRetryContext? retryContext)
        => this with { RetryContext = retryContext };

    /// <summary>Returns a request with a caller-supplied invocation identifier.</summary>
    public FunctionInvocationRequest WithInvocationId(string? invocationId)
        => this with { InvocationId = invocationId };

    private static IReadOnlyDictionary<string, FunctionTestValue> AddValue(
        IReadOnlyDictionary<string, FunctionTestValue> source,
        string name,
        FunctionTestValue value)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A non-empty value name is required.", nameof(name));
        }

        ArgumentNullException.ThrowIfNull(value);
        var result = new Dictionary<string, FunctionTestValue>(source, StringComparer.OrdinalIgnoreCase)
        {
            [name] = value
        };
        return new ReadOnlyDictionary<string, FunctionTestValue>(result);
    }

    private static IReadOnlyDictionary<string, FunctionTestValue> CopyValues(
        IReadOnlyDictionary<string, FunctionTestValue> source,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(source, parameterName);
        var result = new Dictionary<string, FunctionTestValue>(StringComparer.OrdinalIgnoreCase);
        foreach ((string name, FunctionTestValue value) in source)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Value names cannot be empty.", parameterName);
            }

            ArgumentNullException.ThrowIfNull(value, parameterName);
            if (!result.TryAdd(name, value))
            {
                throw new ArgumentException($"Duplicate value name '{name}' ignoring case.", parameterName);
            }
        }

        return new ReadOnlyDictionary<string, FunctionTestValue>(result);
    }
}

/// <summary>Represents W3C trace context supplied to a synthetic invocation.</summary>
public sealed record FunctionTestTraceContext
{
    /// <summary>Initializes an immutable trace context.</summary>
    public FunctionTestTraceContext(
        string traceParent,
        string traceState,
        IReadOnlyDictionary<string, string> attributes,
        IReadOnlyDictionary<string, string> baggage)
    {
        TraceParent = traceParent;
        TraceState = traceState;
        Attributes = CopyStrings(attributes, nameof(attributes));
        Baggage = CopyStrings(baggage, nameof(baggage));
    }

    /// <summary>Gets the W3C trace-parent value.</summary>
    public string TraceParent { get; }

    /// <summary>Gets the W3C trace-state value.</summary>
    public string TraceState { get; }

    /// <summary>Gets immutable trace attributes.</summary>
    public IReadOnlyDictionary<string, string> Attributes { get; }

    /// <summary>Gets immutable baggage.</summary>
    public IReadOnlyDictionary<string, string> Baggage { get; }

    private static IReadOnlyDictionary<string, string> CopyStrings(
        IReadOnlyDictionary<string, string> source,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(source, parameterName);
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach ((string name, string value) in source)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Trace dictionary keys cannot be empty.", parameterName);
            }

            ArgumentNullException.ThrowIfNull(value, parameterName);
            if (!result.TryAdd(name, value))
            {
                throw new ArgumentException($"Duplicate trace key '{name}'.", parameterName);
            }
        }

        return new ReadOnlyDictionary<string, string>(result);
    }
}

/// <summary>Represents retry data visible to function code without scheduling a retry.</summary>
public sealed record FunctionTestRetryContext(
    int RetryCount,
    int MaxRetryCount,
    FunctionInvocationException? PreviousException);

/// <summary>Represents a completed synthetic invocation.</summary>
public sealed record FunctionInvocationResult(
    string InvocationId,
    FunctionInvocationStatus Status,
    FunctionTestValue? ReturnValue,
    IReadOnlyDictionary<string, FunctionTestValue> Outputs,
    FunctionInvocationException? Exception,
    IReadOnlyList<FunctionTestLogEntry> Logs,
    IReadOnlyDictionary<string, string> TraceAttributes)
{
    /// <summary>Throws when the invocation did not succeed.</summary>
    public void EnsureSucceeded()
    {
        if (Status != FunctionInvocationStatus.Succeeded)
        {
            throw new InvalidOperationException(
                Exception?.Message ?? $"Function invocation '{InvocationId}' completed with status '{Status}'.");
        }
    }
}

/// <summary>Identifies the terminal outcome of an invocation.</summary>
public enum FunctionInvocationStatus
{
    /// <summary>The function completed successfully.</summary>
    Succeeded,

    /// <summary>The function failed during conversion, middleware, or user execution.</summary>
    Failed,

    /// <summary>The function observed cancellation.</summary>
    Cancelled
}

/// <summary>Represents an exception returned by the worker.</summary>
public sealed record FunctionInvocationException(
    string Source,
    string Type,
    string Message,
    string StackTrace,
    bool IsUserException);

/// <summary>Worker log severity.</summary>
public enum FunctionTestLogLevel
{
    /// <summary>Trace-level diagnostic detail.</summary>
    Trace,

    /// <summary>Debug-level diagnostic detail.</summary>
    Debug,

    /// <summary>Informational event.</summary>
    Information,

    /// <summary>Warning event.</summary>
    Warning,

    /// <summary>Error event.</summary>
    Error,

    /// <summary>Critical failure event.</summary>
    Critical,

    /// <summary>No log level.</summary>
    None
}

/// <summary>Worker log category.</summary>
public enum FunctionTestLogCategory
{
    /// <summary>User-code log.</summary>
    User,

    /// <summary>Worker-system log.</summary>
    System,

    /// <summary>Custom metric.</summary>
    CustomMetric
}

/// <summary>Represents one log entry associated with an invocation.</summary>
public sealed record FunctionTestLogEntry(
    FunctionTestLogLevel Level,
    FunctionTestLogCategory LogCategory,
    string Category,
    string Message,
    string EventId,
    string JsonProperties,
    FunctionInvocationException? Exception);

/// <summary>Indicates a failure in the in-memory host/worker transport rather than function execution.</summary>
public sealed class FunctionsTestHostException : Exception
{
    /// <summary>Initializes a transport exception.</summary>
    public FunctionsTestHostException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
