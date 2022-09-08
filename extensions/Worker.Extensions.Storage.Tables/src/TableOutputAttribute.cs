using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Attribute used to configure the source of the Azure Storage Tables output binding.
    /// </summary>
    public class TableOutputAttribute : OutputBindingAttribute
    {
        /// <summary>Initializes a new instance of the <see cref="TableOutputAttribute"/> class.</summary>
        /// <param name="tableName">The name of the table to which to bind.</param>
        public TableOutputAttribute(string tableName)
        {
            TableName = tableName;
        }

        /// <summary>Initializes a new instance of the <see cref="TableOutputAttribute"/> class.</summary>
        /// <param name="tableName">The name of the table containing the entity.</param>
        /// <param name="partitionKey">The partition key of the entity.</param>
        public TableOutputAttribute(string tableName, string partitionKey)
        {
            TableName = tableName;
            PartitionKey = partitionKey;
        }

        /// <summary>Initializes a new instance of the <see cref="TableOutputAttribute"/> class.</summary>
        /// <param name="tableName">The name of the table containing the entity.</param>
        /// <param name="partitionKey">The partition key of the entity.</param>
        /// <param name="rowKey">The row key of the entity.</param>
        public TableOutputAttribute(string tableName, string partitionKey, string rowKey)
        {
            TableName = tableName;
            PartitionKey = partitionKey;
            RowKey = rowKey;
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
        /// Gets or sets the app setting name that contains the Azure Storage connection string.
        /// </summary>
        public string? Connection { get; set; }
    }
}
