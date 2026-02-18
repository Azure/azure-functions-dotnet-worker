// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Immutable;
using Microsoft.Build.Execution;

namespace Microsoft.Build.Utilities.ProjectCreation;

internal class TargetOutputs : IReadOnlyDictionary<string, TargetResult>
{
    public static readonly TargetOutputs Empty = new(ImmutableDictionary<string, TargetResult>.Empty);

    private readonly IDictionary<string, TargetResult> _results;

    private TargetOutputs(IDictionary<string, TargetResult> results)
    {
        _results = results ?? ImmutableDictionary<string, TargetResult>.Empty;
    }

    public TargetResult this[string targetName] => _results[targetName];

    public IEnumerable<string> Keys => _results.Keys;

    public IEnumerable<TargetResult> Values => _results.Values;

    public int Count => _results.Count;

    public static TargetOutputs Create(IDictionary<string, TargetResult>? results)
        => results is null ? Empty : new(results);

    public bool ContainsKey(string key) => _results.ContainsKey(key);

    public IEnumerator<KeyValuePair<string, TargetResult>> GetEnumerator()
        => _results.GetEnumerator();

#if NET
    public bool TryGetValue(string key, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out TargetResult value)
        => _results.TryGetValue(key, out value);
#else
    public bool TryGetValue(string key, out TargetResult value)
        => _results.TryGetValue(key, out value);
#endif

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
