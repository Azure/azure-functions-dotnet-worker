// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace ClassicFunctionApp;

public sealed class TestFunction
{
    [Function("ClassicEcho")]
    public string Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData request)
        => "classic";
}
