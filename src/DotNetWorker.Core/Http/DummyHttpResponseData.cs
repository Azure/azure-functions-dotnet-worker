using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Http;

namespace Microsoft.Azure.Functions.Worker.Core.Http
{
    internal class DummyHttpResponseData : HttpResponseData
    {
        public DummyHttpResponseData(FunctionContext functionContext) : base(functionContext)
        {
        }

        public override HttpStatusCode StatusCode { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override HttpHeadersCollection Headers { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override Stream Body { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override HttpCookies Cookies => throw new NotImplementedException();
    }
}
