// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Http
{
    internal class DefaultHttpRequestDataFeature : IHttpRequestDataFeature
    {
        public async ValueTask<HttpRequestData?> GetHttpRequestDataAsync(FunctionContext context)
        {
            var httpTriggerBinding = context.FunctionDefinition.InputBindings.Values
                                            .FirstOrDefault(a => string.Equals(a.Type, "httpTrigger", StringComparison.OrdinalIgnoreCase));

            if (httpTriggerBinding != null)
            {
                var bindingResult = await context.BindInputAsync<HttpRequestData>(httpTriggerBinding);
                return bindingResult.Value;
            }

            return default;
        }
    }
}
