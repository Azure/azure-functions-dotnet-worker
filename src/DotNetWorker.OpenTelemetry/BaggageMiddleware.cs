// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Azure.Functions.Worker.Diagnostics;
using OpenTelemetry;

namespace Microsoft.Azure.Functions.Worker.OpenTelemetry
{
    internal class BaggageMiddleware : IFunctionsWorkerMiddleware
    {
        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            if (context.Items.TryGetValue(TraceConstants.InternalKeys.BaggageKeyName, out var value) &&
                value is IEnumerable<KeyValuePair<string, string>> dict)
            {
                foreach (var kv in dict)
                {
                    Baggage.SetBaggage(kv.Key, kv.Value);
                }
            }

            await next(context);
        }
    }
}
