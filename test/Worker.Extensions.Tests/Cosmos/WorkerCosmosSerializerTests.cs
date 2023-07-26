// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Azure.Core.Serialization;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Extensions.Tests.Cosmos
{
    public class WorkerCosmosSerializerTests
    {
        [Fact]
        public void FromStream_JsonObjectSerializer_ReturnsObject()
        {
            // Arrange
            var objectSerializer = new JsonObjectSerializer();
            var cosmosSerializer = new WorkerCosmosSerializer(objectSerializer);

            Person person = GetTestPerson();
            var expectedJson = JsonSerializer.Serialize<Person>(person);

            using var stream = GenerateStreamFromString(expectedJson);

            // Act
            var result = cosmosSerializer.FromStream<Person>(stream);

            // Assert
            if (expectedJson != JsonSerializer.Serialize<Person>(result))
            {
                Assert.Fail("Objects are not equal");
            }
        }

        [Fact]
        public void FromStream_NewtonsoftJsonObjectSerializer_ReturnsObject()
        {
            // Arrange
            var objectSerializer = new NewtonsoftJsonObjectSerializer();
            var cosmosSerializer = new WorkerCosmosSerializer(objectSerializer);

            Person person = GetTestPerson();
            var expectedJson = JsonSerializer.Serialize<Person>(person);

            using var stream = GenerateStreamFromString(expectedJson);

            // Act
            var result = cosmosSerializer.FromStream<Person>(stream);

            // Assert
            if (expectedJson != JsonSerializer.Serialize<Person>(result))
            {
                Assert.Fail("Objects are not equal");
            }
        }

        [Fact]
        public void ToStream_JsonObjectSerializer_ReturnsStream()
        {
            // Arrange
            var objectSerializer = new JsonObjectSerializer();
            var cosmosSerializer = new WorkerCosmosSerializer(objectSerializer);

            Person person = GetTestPerson();

            // Act
            using var resultStream = cosmosSerializer.ToStream<Person>(person);

            StreamReader sr = new StreamReader(resultStream);
            var resultPerson = JsonSerializer.Deserialize<Person>(sr.ReadToEnd());

            // Assert
            if (JsonSerializer.Serialize<Person>(person) != JsonSerializer.Serialize<Person>(resultPerson))
            {
                Assert.Fail("Objects are not equal");
            }
        }

        [Fact]
        public void ToStream_NewtonsoftJsonObjectSerializer_ReturnsStream()
        {
            // Arrange
            var objectSerializer = new NewtonsoftJsonObjectSerializer();
            var cosmosSerializer = new WorkerCosmosSerializer(objectSerializer);

            Person person = GetTestPerson();

            // Act
            using var resultStream = cosmosSerializer.ToStream<Person>(person);

            StreamReader sr = new StreamReader(resultStream);
            var resultPerson = JsonSerializer.Deserialize<Person>(sr.ReadToEnd());

            // Assert
            if (JsonSerializer.Serialize<Person>(person) != JsonSerializer.Serialize<Person>(resultPerson))
            {
                Assert.Fail("Objects are not equal");
            }
        }

        private static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        private static Person GetTestPerson()
        {
            var children = new List<Person>();
            children.Add(new Person
            {
                Name = "Bob",
                City = City.Seattle,
                Income = 0,
                Children = null,
                Age = 5,
                Guid = Guid.NewGuid()
            });

            return new Person
            {
                Name = "Amy",
                City = City.Seattle,
                Income = 105201,
                Children = children,
                Age = 35,
                Guid = Guid.NewGuid()
            };
        }

        private class Person
        {
            public string Name { get; set; }

            public City City { get; set; }

            public double Income { get; set; }

            public List<Person> Children { get; set; }

            public int Age { get; set; }

            public Guid Guid { get; set; }
        }

        private enum City
        {
            NewYork,
            LosAngeles,
            Seattle
        }
    }
}