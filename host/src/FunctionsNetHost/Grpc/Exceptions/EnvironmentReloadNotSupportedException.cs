﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// The exception that is thrown when the current function app payload does not support environment reload.
    /// </summary>
    public sealed class EnvironmentReloadNotSupportedException : NotSupportedException
    {
        public EnvironmentReloadNotSupportedException() { }

        public EnvironmentReloadNotSupportedException(string message) : base(message) { }
    }
}
