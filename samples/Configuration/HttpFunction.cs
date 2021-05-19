// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text.Json.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Newtonsoft.Json;

namespace Configuration
{
    public class HttpFunction
    {
        [Function("HttpFunction")]
        public static MyType Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req)
        {
            // Shows the effects of changing the JSON handling.
            return new MyType
            {
                camelCasePropertyName = "value",
                PascalCasePropertyName = "value",
                NullValue = null,
                DifferentSerializedName = "value"
            };
        }

        public class MyType
        {
            public string camelCasePropertyName { get; set; }
            public string PascalCasePropertyName { get; set; }
            public string NullValue { get; set; }

            // This sample can use both System.Text.Json and Newtonsoft.Json, depending on which
            // is registered. Including both attributes here.
            [JsonPropertyName("Changed_Via_System_Text_Json")]
            [JsonProperty("Changed_Via_Newtonsoft_Json")]
            public string DifferentSerializedName { get; set; }
        }
    }
}
