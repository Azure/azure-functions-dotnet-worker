using System.Collections.Generic;

namespace FunctionsDotNetWorker.Configuration
{
    public class DotNetWorkerOptions
    {
        public DotNetWorkerOptions()
        {
            Capabilities = new List<string>();

            // Default value for HeartBeatRateFactor is 6
            HeartBeatRateFactor = 6;
        }

        public string? ApplicationId { get; set; }

        public string? ApplicationVersion { get; set; }

        public string? InstanceId { get; set; }

        public List<string> Capabilities { get; }

        public int HeartBeatRateFactor { get; set; }
    }
}
