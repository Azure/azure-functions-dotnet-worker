// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using OpenTelemetry;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.OpenTelemetry.Tests
{
    public class DefaultBaggagePropagatorTests
    {
        [Fact]
        public void SetBaggage_SetsSingleBaggageItem()
        {
            // Arrange
            var propagator = new DefaultBaggagePropagator();
            var baggage = new Dictionary<string, string> { { "key1", "value1" } };

            // Act
            using (propagator.SetBaggage(baggage))
            {
                // Assert
                Assert.Equal("value1", Baggage.GetBaggage("key1"));
            }
        }

        [Fact]
        public void SetBaggage_SetsMultipleBaggageItems()
        {
            // Arrange
            var propagator = new DefaultBaggagePropagator();
            var baggage = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" },
                { "key3", "value3" }
            };

            // Act
            using (propagator.SetBaggage(baggage))
            {
                // Assert
                Assert.Equal("value1", Baggage.GetBaggage("key1"));
                Assert.Equal("value2", Baggage.GetBaggage("key2"));
                Assert.Equal("value3", Baggage.GetBaggage("key3"));
            }
        }

        [Fact]
        public void SetBaggage_HandlesEmptyBaggage()
        {
            // Arrange
            var propagator = new DefaultBaggagePropagator();
            var baggage = new Dictionary<string, string>();

            // Act
            using (propagator.SetBaggage(baggage))
            {
                // Assert - no exception thrown
                Assert.Empty(baggage);
            }
        }

        [Fact]
        public void SetBaggage_DisposingScope_ClearsAllBaggage()
        {
            // Arrange
            var propagator = new DefaultBaggagePropagator();
            var baggage = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" }
            };

            // Act
            var scope = propagator.SetBaggage(baggage);
            Assert.Equal("value1", Baggage.GetBaggage("key1"));
            Assert.Equal("value2", Baggage.GetBaggage("key2"));

            scope?.Dispose();

            // Assert
            Assert.Null(Baggage.GetBaggage("key1"));
            Assert.Null(Baggage.GetBaggage("key2"));
        }

        [Fact]
        public void SetBaggage_UsingStatement_AutomaticallyClearsBaggage()
        {
            // Arrange
            var propagator = new DefaultBaggagePropagator();
            var baggage = new Dictionary<string, string> { { "key1", "value1" } };

            // Act
            using (propagator.SetBaggage(baggage))
            {
                Assert.Equal("value1", Baggage.GetBaggage("key1"));
            }

            // Assert - baggage cleared after scope
            Assert.Null(Baggage.GetBaggage("key1"));
        }
    }
}
