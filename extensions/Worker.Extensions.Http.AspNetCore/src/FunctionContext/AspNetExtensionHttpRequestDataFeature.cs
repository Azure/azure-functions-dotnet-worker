// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Http;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore
{
    internal class AspNetExtensionHttpRequestDataFeature : IHttpRequestDataFeature
    {
        public ValueTask<HttpRequestData?> GetHttpRequestDataAsync(FunctionContext context)
        {
            throw new NotSupportedException($"The method {nameof(GetHttpRequestDataAsync)} " +
                $"is not supported under the dotnet-isolated ASP.NET core integration model.");
        }
    }
}
