// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;

namespace Microsoft.Azure.Functions.Worker
{
    internal static class ApplicationDirectory
    {
        private static Lazy<string> _current = CreateValue();

        public static string Current => _current.Value;

        internal static void ResetForTests() => Interlocked.Exchange(ref _current, CreateValue());

        private static Lazy<string> CreateValue()
            => new(
                () =>
                {
                    string? directory = Environment.GetEnvironmentVariable("FUNCTIONS_APPLICATION_DIRECTORY");
                    return string.IsNullOrWhiteSpace(directory) ? AppContext.BaseDirectory : directory;
                },
                LazyThreadSafetyMode.ExecutionAndPublication);
    }
}
