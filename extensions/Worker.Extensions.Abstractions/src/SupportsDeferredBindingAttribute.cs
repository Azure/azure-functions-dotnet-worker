// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Extensions.Abstractions
{
    /// <summary>
    /// Specifies if a converter supports deferred binding when generating function metadata.
    /// This is to be used on converters that support deferred binding.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class SupportsDeferredBindingAttribute : Attribute
    {
    }
}
