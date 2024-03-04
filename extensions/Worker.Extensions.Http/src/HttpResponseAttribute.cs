﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Attribute used to mark an HTTP Response on an HTTP Trigger function with multiple output bindings.
    /// </summary>
    public sealed class HttpResponseAttribute : TriggerBindingAttribute
    {
        /// <summary>
        /// Creates an instance of the <see cref="HttpResponseAttribute"/>.
        /// </summary>
        public HttpResponseAttribute()
        {
        }
    }
}
