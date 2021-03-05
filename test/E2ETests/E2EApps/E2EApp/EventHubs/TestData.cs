using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.E2EApp.EventHubs
{
    public class TestData
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("TimeProperty")]
        public string TimeProperty { get; set; }

        public override string ToString()
        {
            return $"Name: {Name}, TimeProperty: {TimeProperty}";
        }
    }
}
