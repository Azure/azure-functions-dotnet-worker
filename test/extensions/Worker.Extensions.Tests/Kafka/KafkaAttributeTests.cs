// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

    }
}
namespace Microsoft.Azure.Functions.Worker.Extensions.Tests.Kafka
{
    public class KafkaAttributeTests
    {
        [Fact]
        public void KafkaTriggerAttributeSupportsOAuthHttpsCaSettings()
        {
            var attribute = new KafkaTriggerAttribute("broker", "topic")
            {
                HttpsCaLocation = "ca-path",
                HttpsCaPem = "ca-pem"
            };

            Assert.Equal("ca-path", attribute.HttpsCaLocation);
            Assert.Equal("ca-pem", attribute.HttpsCaPem);
        }

        [Fact]
        public void KafkaOutputAttributeSupportsOAuthHttpsCaSettings()
        {
            var attribute = new KafkaOutputAttribute("broker", "topic")
            {
                HttpsCaLocation = "ca-path",
                HttpsCaPem = "ca-pem"
            };

            Assert.Equal("ca-path", attribute.HttpsCaLocation);
            Assert.Equal("ca-pem", attribute.HttpsCaPem);
        }
    }
}