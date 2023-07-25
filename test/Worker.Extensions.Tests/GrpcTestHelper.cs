// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Google.Protobuf;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker.Extensions.Tests
{
    public static class GrpcTestHelper
    {
        internal static GrpcModelBindingData GetTestGrpcModelBindingData(BinaryData content, string source, string contentType = "application/json")
        {
            var data = new ModelBindingData()
            {
                Version = "1.0",
                Source = source,
                Content = ByteString.CopyFrom(content),
                ContentType = contentType
            };

            return new GrpcModelBindingData(data);
        }
    }
}