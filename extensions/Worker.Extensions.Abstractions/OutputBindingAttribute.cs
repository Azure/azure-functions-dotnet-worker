// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using System;

namespace Microsoft.Azure.Functions.Worker.Extensions.Abstractions
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    public abstract class OutputBindingAttribute : BindingAttribute
    {
        public OutputBindingAttribute()
        {
        }

        public OutputBindingAttribute(string name)
        {
            Name = name;
        }

        public string? Name { get; }
    }
}
