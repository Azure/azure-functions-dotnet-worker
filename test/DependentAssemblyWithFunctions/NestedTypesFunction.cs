﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace MyCompany.MyProduct.MyApp
{
    public sealed class Foo
    {
        public sealed class Bar
        {
            [Function("NestedTypeFunc")]
            public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
            {
                throw new NotImplementedException();
            }
        }
    }
}
