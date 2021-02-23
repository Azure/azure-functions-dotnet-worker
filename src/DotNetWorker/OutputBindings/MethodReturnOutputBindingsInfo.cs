// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.OutputBindings
{
    internal class MethodReturnOutputBindingsInfo : OutputBindingsInfo
    {
        public override void BindOutputInContext(FunctionContext context)
        {
            // For output bindings that are defined by method returns,
            // the invocation result provides the value. The $return binding name
            // is sufficient to have it identified that the output binding value
            // comes from function invocation result.
        }
    }
}
