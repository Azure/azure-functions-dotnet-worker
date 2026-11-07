// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Testing;

/// <summary>Represents one protocol-neutral value supplied to or returned from a function invocation.</summary>
public abstract record FunctionTestValue
{
    /// <summary>Creates an explicit null value.</summary>
    public static FunctionTestValue Null() => new NullValue();

    /// <summary>Creates a string value.</summary>
    public static FunctionTestValue String(string value) => new StringValue(value);

    /// <summary>Creates a JSON value.</summary>
    public static FunctionTestValue Json(string utf8Json) => new JsonValue(utf8Json);

    /// <summary>Creates a binary value.</summary>
    public static FunctionTestValue Bytes(ReadOnlyMemory<byte> value) => new BytesValue(value);

    /// <summary>Creates a 64-bit integer value.</summary>
    public static FunctionTestValue Int64(long value) => new Int64Value(value);

    /// <summary>Creates a double-precision value.</summary>
    public static FunctionTestValue Double(double value) => new DoubleValue(value);

    /// <summary>Materializes a stream into a binary value before invocation dispatch.</summary>
    public static async Task<FunctionTestValue> FromStreamAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        return Bytes(buffer.ToArray());
    }

    /// <summary>An explicit null value.</summary>
    public sealed record NullValue : FunctionTestValue;

    /// <summary>A string value.</summary>
    public sealed record StringValue(string Value) : FunctionTestValue;

    /// <summary>A JSON document represented as text.</summary>
    public sealed record JsonValue(string Utf8Json) : FunctionTestValue;

    /// <summary>A binary value.</summary>
    public sealed record BytesValue(ReadOnlyMemory<byte> Value) : FunctionTestValue;

    /// <summary>A 64-bit integer value.</summary>
    public sealed record Int64Value(long Value) : FunctionTestValue;

    /// <summary>A double-precision value.</summary>
    public sealed record DoubleValue(double Value) : FunctionTestValue;

    /// <summary>A string collection.</summary>
    public sealed record StringCollection(IReadOnlyList<string> Values) : FunctionTestValue;

    /// <summary>A binary collection.</summary>
    public sealed record BytesCollection(IReadOnlyList<ReadOnlyMemory<byte>> Values) : FunctionTestValue;

    /// <summary>A 64-bit integer collection.</summary>
    public sealed record Int64Collection(IReadOnlyList<long> Values) : FunctionTestValue;

    /// <summary>A double-precision collection.</summary>
    public sealed record DoubleCollection(IReadOnlyList<double> Values) : FunctionTestValue;

    /// <summary>A model-binding payload.</summary>
    public sealed record ModelBindingValue(FunctionTestModelBindingData Value) : FunctionTestValue;

    /// <summary>A collection of model-binding payloads.</summary>
    public sealed record ModelBindingCollection(IReadOnlyList<FunctionTestModelBindingData> Values) : FunctionTestValue;
}

/// <summary>Represents one SDK model-binding payload.</summary>
public sealed record FunctionTestModelBindingData(
    string Version,
    string Source,
    string ContentType,
    ReadOnlyMemory<byte> Content);
