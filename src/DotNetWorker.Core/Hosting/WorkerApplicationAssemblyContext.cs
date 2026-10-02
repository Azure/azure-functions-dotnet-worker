// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Threading;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Carries the function application assembly through entry-point execution without
    /// changing process-wide state. This is consumed by the repository-owned testing
    /// package before modern builders eagerly execute generated startup code.
    /// </summary>
    internal static class WorkerApplicationAssemblyContext
    {
        private static readonly AsyncLocal<Scope?> _current = new();

        internal static Assembly ResolveOrEntryAssembly()
        {
            return _current.Value?.ApplicationAssembly
                ?? Assembly.GetEntryAssembly()
                ?? throw new InvalidOperationException("Unable to resolve the function application assembly because the process entry assembly is unavailable.");
        }

        internal static IDisposable Push(Assembly applicationAssembly)
        {
            if (applicationAssembly is null)
            {
                throw new ArgumentNullException(nameof(applicationAssembly));
            }

            Scope? previous = _current.Value;
            if (previous is not null && previous.ApplicationAssembly != applicationAssembly)
            {
                throw new InvalidOperationException(
                    $"The function application assembly is already scoped to '{previous.ApplicationAssembly.FullName}' and cannot be changed to '{applicationAssembly.FullName}' within the same execution context.");
            }

            var scope = new Scope(applicationAssembly, previous);
            _current.Value = scope;
            return scope;
        }

        private sealed class Scope : IDisposable
        {
            private readonly Scope? _previous;
            private bool _disposed;

            internal Scope(Assembly applicationAssembly, Scope? previous)
            {
                ApplicationAssembly = applicationAssembly;
                _previous = previous;
            }

            internal Assembly ApplicationAssembly { get; }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                if (!ReferenceEquals(_current.Value, this))
                {
                    throw new InvalidOperationException("Function application assembly scopes must be disposed in reverse order.");
                }

                _current.Value = _previous;
                _disposed = true;
            }
        }
    }
}
