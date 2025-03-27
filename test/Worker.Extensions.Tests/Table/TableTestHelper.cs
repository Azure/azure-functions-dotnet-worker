// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Extensions.Tests.Table
{
    internal static class TableTestHelper
    {
        public static BinaryData GetWrongBinaryData()
        {
            return new BinaryData("{" + "\"Connection\" : \"Connection\"" + "}");
        }

        public static BinaryData GetTableClientBinaryData()
        {
            return new BinaryData("{" +
                "\"TableName\" : \"TableName\"" +
                "}");
        }

        public static BinaryData GetTableEntityBinaryData()
        {
            return new BinaryData("{" +
                "\"Connection\" : \"Connection\"," +
                "\"TableName\" : \"TableName\"," +
                "\"PartitionKey\" : \"PartitionKey\"," +
                "\"RowKey\" : \"RowKey\"" +
                "}");
        }

        public static BinaryData GetEntityWithoutRowKeyBinaryData()
        {
            return new BinaryData("{" +
                "\"Connection\" : \"Connection\"," +
                "\"TableName\" : \"TableName\"," +
                "\"PartitionKey\" : \"PartitionKey\"" +
                "}");
        }
    }
}
