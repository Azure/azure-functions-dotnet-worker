// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Extensions.Tests.Blob
{
    public class BlobTestHelper
    {
        public static BinaryData GetTestBinaryData(string connection = "Connection", string container = "Container", string blobName = "")
        {
            string jsonData = $@"{{
                                ""Connection"" : ""{connection}"",
                                ""ContainerName"" : ""{container}"",
                                ""BlobName"" : ""{blobName}""
                            }}";

            return new BinaryData(jsonData);
        }
    }
}