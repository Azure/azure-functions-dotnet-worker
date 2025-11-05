// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Functions.Worker.Extensions.CosmosDB;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Extensions.Tests.Cosmos
{
    public class UtilitiesTests
    {
        [Theory]
        [InlineData("westeurope,eastus")]
        [InlineData("westeurope , eastus")]
        [InlineData(" westeurope, eastus ")]
        public void ParsePreferredLocations_ValidInput_ReturnsList(string input)
        {
            // Arrange
            var expectedList = new List<string>();
            expectedList.Add("westeurope");
            expectedList.Add("eastus");

            // Act
            var result = Utilities.ParsePreferredLocations(input);

            //Assert
            Assert.Equal(expectedList, result);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void ParsePreferredLocations_InvalidInput_ReturnsEmptyList(string input)
        {
            // Arrange
            var expectedList = Enumerable.Empty<string>().ToList();

            // Act
            var result = Utilities.ParsePreferredLocations(input);

            //Assert
            Assert.Equal(expectedList, result);
        }
    }
}