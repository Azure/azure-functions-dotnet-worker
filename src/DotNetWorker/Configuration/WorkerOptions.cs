// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Azure.Functions.Worker.Configuration
{
    // TODO: Slim this down.
    public class WorkerOptions
    {
        public WorkerOptions()
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
