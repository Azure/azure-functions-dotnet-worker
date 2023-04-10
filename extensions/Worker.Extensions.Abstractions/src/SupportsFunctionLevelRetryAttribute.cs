// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Extensions.Abstractions
{
    /// <summary>
    /// Specifies if a binding attribute supports a retry policy at the function level.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class SupportsFunctionLevelRetryAttribute : Attribute
    {
    }
}
