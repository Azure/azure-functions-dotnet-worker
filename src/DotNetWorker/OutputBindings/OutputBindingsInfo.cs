// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.OutputBindings
{
    /// <summary>
    /// Encapsulates the information about all output bindings in a Function
    /// </summary>
    public abstract class OutputBindingsInfo
    {
        /// <summary>
        /// Binds output from a function <paramref name="context"/> to the output bindings
        /// </summary>
        /// <param name="context">The Function context to bind the data to.</param>
        public abstract void BindOutputInContext(FunctionContext context);
    }
}
