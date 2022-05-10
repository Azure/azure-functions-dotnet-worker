using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs
{
    public class TableOutputAttribute : OutputBindingAttribute
    {
        private readonly string _tableName;
        private readonly string _partitionKey;
        private readonly string _rowKey;

        /// <summary>Initializes a new instance of the <see cref="TableAttribute"/> class.</summary>
        /// <param name="tableName">The name of the table to which to bind.</param>
        public TableOutputAttribute(string tableName)
        {
            _tableName = tableName;
        }

        /// <summary>Initializes a new instance of the <see cref="TableAttribute"/> class.</summary>
        /// <param name="tableName">The name of the table containing the entity.</param>
        /// <param name="partitionKey">The partition key of the entity.</param>
        public TableOutputAttribute(string tableName, string partitionKey)
        {
            _tableName = tableName;
            _partitionKey = partitionKey;
        }

        /// <summary>Initializes a new instance of the <see cref="TableAttribute"/> class.</summary>
        /// <param name="tableName">The name of the table containing the entity.</param>
        /// <param name="partitionKey">The partition key of the entity.</param>
        /// <param name="rowKey">The row key of the entity.</param>
        public TableOutputAttribute(string tableName, string partitionKey, string rowKey)
        {
            _tableName = tableName;
            _partitionKey = partitionKey;
            _rowKey = rowKey;
        }

        /// <summary>Gets the name of the table to which to bind.</summary>
        /// <remarks>When binding to a table entity, gets the name of the table containing the entity.</remarks>
        public string TableName
        {
            get { return _tableName; }
        }

        /// <summary>When binding to a table entity, gets the partition key of the entity.</summary>
        /// <remarks>When binding to an entire table, returns <see langword="null"/>.</remarks>
        public string PartitionKey
        {
            get { return _partitionKey; }
        }

        /// <summary>When binding to a table entity, gets the row key of the entity.</summary>
        /// <remarks>When binding to an entire table, returns <see langword="null"/>.</remarks>
        public string RowKey
        {
            get { return _rowKey; }
        }

        /// <summary>
        /// Gets or sets the app setting name that contains the Azure Storage connection string.
        /// </summary>
        public string Connection { get; set; }
    }
}
