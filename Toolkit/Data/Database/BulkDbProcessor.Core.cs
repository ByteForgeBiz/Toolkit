using System;
using System.Collections.Generic;
using System.Reflection;

namespace ByteForge.Toolkit
{
    /*
     *  ___      _ _   ___  _    ___                                 _________  
     * | _ )_  _| | |_|   \| |__| _ \_ _ ___  __ ___ ______ ___ _ _ / /_   _\ \ 
     * | _ \ || | | / / |) | '_ \  _/ '_/ _ \/ _/ -_)_-<_-</ _ \ '_< <  | |  > >
     * |___/\_,_|_|_\_\___/|_.__/_| |_| \___/\__\___/__/__/\___/_|  \_\ |_| /_/ 
     *                                                                          
     */
    /// <summary>
    /// Provides generic batch database operations for bulk inserting, upserting, and deleting records of type <typeparamref name="T"/>.<br/>
    /// Supports SQL Server bulk operations and dynamic table creation based on property mappings.
    /// </summary>
    /// <typeparam name="T">The type of records to be processed.</typeparam>
    public partial class BulkDbProcessor<T>
    {
        // Fields (sorted)
        private const int DEFAULT_VARCHAR_LENGTH = 255;
        private const int DefaultBatchSize = 1000;
        private const int SQL2000_TEXT_THRESHOLD = 8000;
        private bool _dropTableIfExists = true;

        // Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="BulkDbProcessor{T}"/> class.
        /// </summary>
        protected BulkDbProcessor()
        {
            InitializePropertyMapping();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BulkDbProcessor{T}"/> class with the specified destination table
        /// name.
        /// </summary>
        /// <param name="destinationTableName">The name of the destination table where data will be processed.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="destinationTableName"/> is null, empty, or consists only of whitespace.</exception>
        public BulkDbProcessor(string destinationTableName) : this()
        {
            if (string.IsNullOrWhiteSpace(destinationTableName))
                throw new ArgumentException("Destination table name cannot be null or empty", nameof(destinationTableName));
            DestinationTableName = destinationTableName;
        }

        // Properties (sorted)
        /// <summary>
        /// Gets the batch size for bulk insert operations. Default is 1000 records.
        /// </summary>
        /// <remarks>
        /// This property determines how many records are sent to the database in a single batch during bulk operations.
        /// Can be overridden in derived classes to customize batch sizing.
        /// </remarks>
        protected virtual int BatchSize => DefaultBatchSize;

        /// <summary>
        /// Gets or sets a value indicating whether null strings should be converted to empty strings during processing.
        /// </summary>
        public bool NullStringsAreEmpty { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the destination table should be created.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the destination table should be created; otherwise, <see langword="false"/>.
        /// Default is <see langword="true"/>.
        /// </value>
        /// <remarks>
        /// When <see langword="true"/>, the processor will create the destination table if it doesn't already exist.
        /// When <see langword="false"/>, the processor assumes the table already exists in the database.
        /// </remarks>
        public bool CreateDestinationTable { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the destination table should be dropped if it already exists.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the destination table should be dropped if it already exists; otherwise, <see langword="false"/>.
        /// Default is <see langword="true"/>.
        /// </value>
        /// <remarks>
        /// When <see langword="true"/>, the processor will drop the destination table if it already exists and recreate it.
        /// When <see langword="false"/>, the processor will use the existing table structure.
        /// Setting this property to <see langword="true"/> automatically sets <see cref="CreateDestinationTable"/> to <see langword="true"/>.
        /// </remarks>
        public bool DropDestinationTableIfExists
        {
            get => _dropTableIfExists;
            set
            {
                _dropTableIfExists = value;
                CreateDestinationTable = value || CreateDestinationTable;
            }
        }

        /// <summary>
        /// Gets the timeout in seconds for bulk copy operations. Default is 600 seconds (10 minutes).
        /// </summary>
        /// <remarks>
        /// This property controls how long SqlBulkCopy operations will wait before timing out.
        /// Can be overridden in derived classes to customize timeout behavior.
        /// </remarks>
        public int BulkCopyTimeout { get; set; } = 600;

        /// <summary>
        /// Gets the cached property to column mapping.
        /// </summary>
        /// <remarks>
        /// This dictionary maps PropertyInfo objects to their corresponding database column names.
        /// It is initialized by <see cref="InitializePropertyMapping"/> based on DBColumnAttribute settings.
        /// </remarks>
        protected Dictionary<PropertyInfo, string> ColumnMap { get; private set; }

        /// <summary>
        /// Gets the name of the destination table for bulk insert operations.
        /// </summary>
        /// <value>
        /// The name of the destination table in the database.
        /// </value>
        /// <remarks>
        /// This is the table where records will be inserted, updated, or deleted from during bulk operations.
        /// </remarks>
        public string DestinationTableName { get; }

        /// <summary>
        /// Gets the names of the index columns.
        /// </summary>
        /// <value>
        /// An array of column names that have regular (non-unique) indexes.
        /// </value>
        /// <remarks>
        /// These are determined from properties with <see cref="DBColumnAttribute.HasIndex"/> set to <see langword="true"/>,
        /// excluding those that are already part of primary keys or unique indexes.
        /// </remarks>
        public string[] Indexes { get; private set; }

        /// <summary>
        /// Gets the cached properties for the current mapping.
        /// </summary>
        /// <value>
        /// An array of PropertyInfo objects for properties decorated with <see cref="DBColumnAttribute"/>.
        /// </value>
        /// <remarks>
        /// These properties are used to map between entity properties and database columns.
        /// </remarks>
        protected PropertyInfo[] Properties { get; private set; }

        /// <summary>
        /// Gets the names of the primary key columns.
        /// </summary>
        /// <value>
        /// An array of column names that form the primary key of the table.
        /// </value>
        /// <remarks>
        /// These are determined from properties with <see cref="DBColumnAttribute.IsPrimaryKey"/> set to <see langword="true"/>.
        /// </remarks>
        public string[] PrimaryKeys { get; private set; }

        /// <summary>
        /// Gets the names of the unique index columns.
        /// </summary>
        /// <value>
        /// An array of column names that have unique indexes but are not part of the primary key.
        /// </value>
        /// <remarks>
        /// These are determined from properties with <see cref="DBColumnAttribute.IsUnique"/> set to <see langword="true"/>
        /// that are not also primary keys.
        /// </remarks>
        public string[] UniqueIndexes { get; private set; }

        /// <summary>
        /// Gets the most recent exception encountered during the operation of the application.
        /// </summary>
        /// <remarks>
        /// This property is updated whenever an exception is caught and logged by the application.  
        /// It can be used to inspect the details of the most recent error for debugging or logging purposes.
        /// </remarks>
        public Exception LastException { get; private set; }
    }
}