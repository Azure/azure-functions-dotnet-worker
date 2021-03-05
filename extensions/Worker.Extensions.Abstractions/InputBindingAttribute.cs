// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using System;

namespace Microsoft.Azure.Functions.Worker.Extensions.Abstractions
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public abstract class InputBindingAttribute : BindingAttribute
    {
        public InputBindingAttribute()
        {
        }
    }
}
