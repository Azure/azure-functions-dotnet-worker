// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Extensions.Tables.TypeConverters;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Attribute used to configure a parameter as the input target for the Azure Storage Tables binding.
    /// </summary>
    [InputConverter(typeof(TableClientConverter))]
    [InputConverter(typeof(TableEntityConverter))]
    [InputConverter(typeof(TableEntityEnumerableConverter))]
    [InputConverter(typeof(TablePocoConverter))]
    [ConverterFallbackBehavior(ConverterFallbackBehavior.Default)]
    public class TableInputAttribute : InputBindingAttribute
    {
        /// <summary>Initializes a new instance of the <see cref="TableInputAttribute"/> class.</summary>
        /// <param name="tableName">The name of the table to which to bind.</param>
        public TableInputAttribute(string tableName)
        {
            TableName = tableName;
        }

        /// <summary>Initializes a new instance of the <see cref="TableInputAttribute"/> class.</summary>
        /// <param name="tableName">The name of the table containing the entity.</param>
        /// <param name="partitionKey">The partition key of the entity.</param>
        public TableInputAttribute(string tableName, string partitionKey)
        {
            TableName = tableName;
            PartitionKey = partitionKey;
        }

        /// <summary>Initializes a new instance of the <see cref="TableInputAttribute"/> class.</summary>
        /// <param name="tableName">The name of the table containing the entity.</param>
        /// <param name="partitionKey">The partition key of the entity.</param>
        /// <param name="rowKey">The row key of the entity.</param>
        public TableInputAttribute(string tableName, string partitionKey, string rowKey)
        {
            TableName = tableName;
            PartitionKey = partitionKey;
            RowKey = rowKey;
        }

        /// <summary>Initializes a new instance of the <see cref="TableInputAttribute"/> class.</summary>
        /// <param name="tableName">The name of the table containing the entity.</param>
        /// <param name="partitionKey">The partition key of the entity.</param>
        /// <param name="take">The number of entities to return </param>
        public TableInputAttribute(string tableName, string partitionKey, int take)
        {
            TableName = tableName;
            PartitionKey = partitionKey;
            Take = take;
        }

        /// <summary>Gets the name of the table to which to bind.</summary>
        /// <remarks>When binding to a table entity, gets the name of the table containing the entity.</remarks>
        public string TableName { get; }

        /// <summary>When binding to a table entity, gets the partition key of the entity.</summary>
        /// <remarks>When binding to an entire table, returns <see langword="null"/>.</remarks>
        public string? PartitionKey { get; }

        /// <summary>When binding to a table entity, gets the row key of the entity.</summary>
        /// <remarks>When binding to an entire table, returns <see langword="null"/>.</remarks>
        public string? RowKey { get; }

        /// <summary>
        /// Allow arbitrary table filter. RowKey should be null. 
        /// </summary>
        public string? Filter
        {
            get; set;
        }

        /// <summary>
        /// Used with filter. RowKey should be null. 
        /// </summary>
        public int Take
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the app setting name that contains the Azure Storage connection string.
        /// </summary>
        public string? Connection { get; set; }
    }
}
