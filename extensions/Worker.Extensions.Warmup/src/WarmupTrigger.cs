// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Attribute used to mark a function that should be invoked during the warmup 
    /// stage of the Function App.
    /// </summary>
    public sealed class WarmupTrigger : TriggerBindingAttribute
    {
    }
}
