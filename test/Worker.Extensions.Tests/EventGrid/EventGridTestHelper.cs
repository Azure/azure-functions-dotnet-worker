// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Google.Protobuf;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker.Extensions.Tests
{
    internal static class EventGridTestHelper
    {
        public static string GetEventGridJsonData(string id = "2947780a-356b-c5a5-feb4-f5261fb2f155", string song = "Vampire")
        {
            return $@"{{
                        ""specversion"" : ""1.0"",
                        ""id"" : ""{id}"",
                        ""type"" : ""UnitTestData"",
                        ""source"" : ""UnitTest"",
                        ""subject"" : ""Song"",
                        ""time"" : ""2020-09-14T10:00:00Z"",
                        ""data"" : {{ ""artist"":""Olivia Rodrigo"",""song"":""{song}"" }}
                    }}";
        }

        public static string GetEventGridJsonDataArray()
        {
            return $@"[
                {GetEventGridJsonData("2947780a-356b-c5a5-feb4-f5261fb2f155", "Driver's License")},
                {GetEventGridJsonData("b85d631a-101e-005a-02f2-cee7aa06f148", "Deja Vu")}
            ]";
        }

        public static string GetEventGridEventJsonData(string id = "2947780a-356b-c5a5-feb4-f5261fb2f155", string song = "Vampire")
        {
            return $@"{{
                        ""id"" : ""{id}"",
                        ""topic"" : ""UnitTestData"",
                        ""subject"" : ""Song"",
                        ""eventType"" : ""MyEvent"",
                        ""eventTime"" : ""2020-09-14T10:00:00Z"",
                        ""data"" : {{ ""artist"":""Olivia Rodrigo"",""song"":""{song}"" }},
                        ""dataVersion"" : ""1.0""
                    }}";
        }

        public static string GetEventGridEventJsonDataArray()
        {
            return $@"[
                {GetEventGridEventJsonData("2947780a-356b-c5a5-feb4-f5261fb2f155", "Driver's License")},
                {GetEventGridEventJsonData("b85d631a-101e-005a-02f2-cee7aa06f148", "Deja Vu")}
            ]";
        }
    }
}