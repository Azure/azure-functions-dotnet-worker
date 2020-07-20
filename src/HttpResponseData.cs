using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunctionsDotNetWorker
{
    public class HttpResponseData
    {
        public HttpResponseData() { }

        public HttpResponseData(string statusCode, string body)
        {
            StatusCode = statusCode;
            Body = body;
            Headers = new Dictionary<string, string>();
        }

        public string StatusCode { get; set; }
        public string Body { get; set; }
        public Dictionary<string, string> Headers { get; set; }
    }
}
