using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.Azure.Functions.Worker.Core.Diagnostics
{
    internal class HostTelemetryEvent
    {
        public HostTelemetryEvent(IDictionary<string, string> payload)
        {
            Payload = new ReadOnlyDictionary<string, string>(payload);
        }

        public string Id { get; set; }
        public string InvocationId { get; set; }
        public string FunctionName { get; set; }
        public string EventName { get; set; }

        public IReadOnlyDictionary<string, string> Payload { get; }
    }
}
