// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.AspNetMiddleware;

internal interface IFunctionsHttpRequestDispatcher
{
    Task DispatchAsync(HttpContext context, RequestDelegate next);
}
