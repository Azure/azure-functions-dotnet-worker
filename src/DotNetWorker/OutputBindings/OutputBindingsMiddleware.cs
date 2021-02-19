// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Pipeline;

namespace Microsoft.Azure.Functions.Worker.OutputBindings
{
    internal sealed class OutputBindingsMiddleware
    {
        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            await next(context);

            AddOutputBindings(context);
        }

        internal static void AddOutputBindings(FunctionContext context)
        {
            object? result = context.InvocationResult;

            if (result != null)
            {
                OutputBindingsInfo outputBindingsInfo = context.FunctionDefinition.OutputBindingsInfo;

                // This means that whatever invocation result was produced by the function was actually
                // used to bind to output bindings. Because the data is binded, we don't need to set any
                // invocation result here.
                // TODO: This may need to be cleaned up.
                if (outputBindingsInfo.BindDataToDictionary(context.OutputBindings, result))
                {
                    context.InvocationResult = null;
                }
            }
        }
    }
}
