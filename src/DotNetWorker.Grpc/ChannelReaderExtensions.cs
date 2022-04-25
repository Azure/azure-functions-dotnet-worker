// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// This is excluded in the projec for other targets,
// but conditionally compiling for clarity.
// This implementation will be used in .NET Standard 2.0

#if NETSTANDARD2_0
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;

namespace Microsoft.Azure.Functions.Worker.Grpc
{
    internal static class ChannelReaderExtensions
    {
        public static async IAsyncEnumerable<T> ReadAllAsync<T>(this ChannelReader<T> reader, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                while (reader.TryRead(out T? item))
                {
                    yield return item;
                }
            }
        }
    }
}
#endif
