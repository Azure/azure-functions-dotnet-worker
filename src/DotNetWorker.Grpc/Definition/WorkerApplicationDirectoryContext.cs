// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;

namespace Microsoft.Azure.Functions.Worker.Definition;

internal static class WorkerApplicationDirectoryContext
{
    private const string FunctionsWorkerDirectoryKey = "FUNCTIONS_WORKER_DIRECTORY";
    private const string FunctionsApplicationDirectoryKey = "FUNCTIONS_APPLICATION_DIRECTORY";
    private static readonly AsyncLocal<Scope?> Current = new();

    internal static string ResolveOrEnvironment()
    {
        string? directory = Current.Value?.Directory
            ?? Environment.GetEnvironmentVariable(FunctionsApplicationDirectoryKey)
            ?? Environment.GetEnvironmentVariable(FunctionsWorkerDirectoryKey);

        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException(
                $"The '{FunctionsApplicationDirectoryKey}' environment variable value is not defined. This is a required environment variable that is automatically set by the Azure Functions runtime.");
        }

        return directory;
    }

    internal static IDisposable Push(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new ArgumentException("A function application directory is required.", nameof(directory));
        }

        Scope? previous = Current.Value;
        if (previous is not null && !string.Equals(previous.Directory, directory, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"The function application directory is already scoped to '{previous.Directory}' and cannot be changed to '{directory}' within the same execution context.");
        }

        var scope = new Scope(directory, previous);
        Current.Value = scope;
        return scope;
    }

    private sealed class Scope : IDisposable
    {
        private readonly Scope? _previous;
        private bool _disposed;

        internal Scope(string directory, Scope? previous)
        {
            Directory = directory;
            _previous = previous;
        }

        internal string Directory { get; }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (!ReferenceEquals(Current.Value, this))
            {
                throw new InvalidOperationException("Function application directory scopes must be disposed in reverse order.");
            }

            Current.Value = _previous;
            _disposed = true;
        }
    }
}
