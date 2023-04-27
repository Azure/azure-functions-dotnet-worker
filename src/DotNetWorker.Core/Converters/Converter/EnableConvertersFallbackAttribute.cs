// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    /// <summary>
    /// An attribute that can specify if Converters fallback is enabled
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class EnableConvertersFallbackAttribute : Attribute
    {
    }
}
