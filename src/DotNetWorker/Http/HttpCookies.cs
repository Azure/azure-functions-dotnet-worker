// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.Http
{
    public abstract class HttpCookies
    {
        public abstract void Append(string name, string value);

        public abstract void Append(IHttpCookie cookie);

        public abstract IHttpCookie CreateNew();
    }
}
