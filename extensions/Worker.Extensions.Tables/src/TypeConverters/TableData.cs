// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.Extensions.Tables.TypeConverters
{
    internal class TableData
    {
        public string? TableName { get; set; }
        public string? Connection { get; set; }
        public string? PartitionKey { get; set; }
        public string? RowKey { get; set; }
        public int Take { get; set; }
        public string? Filter { get; set; }
    }
}
