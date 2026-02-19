// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Middleware;
using OpenTelemetry;

namespace Microsoft.Azure.Functions.Worker.OpenTelemetry
{
    internal class BaggageMiddleware : IFunctionsWorkerMiddleware
    {
        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            try
            {
                foreach (var kv in context.TraceContext.Baggage)
                {
                    Baggage.SetBaggage(kv.Key, kv.Value);
                }

                await next(context);
            }
            finally
            {
                Baggage.ClearBaggage();
            }

        }
    }
}
