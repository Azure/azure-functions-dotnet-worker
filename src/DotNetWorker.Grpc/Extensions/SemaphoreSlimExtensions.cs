// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Grpc;

/// <summary>
/// Provides disposable ownership of an asynchronously acquired semaphore.
/// </summary>
internal static class SemaphoreSlimExtensions
{
    extension(SemaphoreSlim semaphore)
    {
        /// <summary>
        /// Acquires the semaphore before creating its release guard, so cancelled waits never release another owner's acquisition.
        /// </summary>
        internal async ValueTask<IDisposable> LockAsync(CancellationToken cancellationToken)
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            return new Releaser(semaphore);
        }
    }

    /// <summary>
    /// Owns one successful acquisition and releases it at most once.
    /// </summary>
    private sealed class Releaser(SemaphoreSlim semaphore) : IDisposable
    {
        private SemaphoreSlim? _semaphore = semaphore;

        /// <summary>
        /// Atomically clears ownership before releasing, making repeated disposal harmless.
        /// </summary>
        public void Dispose() => Interlocked.Exchange(ref _semaphore, null)?.Release();
    }
}
