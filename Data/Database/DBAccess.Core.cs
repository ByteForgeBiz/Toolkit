using System;

namespace ByteForge.Toolkit
{
    /*
     *  ___  ___   _                   
     * |   \| _ ) /_\  __ __ ___ ______
     * | |) | _ \/ _ \/ _/ _/ -_)_-<_-<
     * |___/|___/_/ \_\__\__\___/__/__/
     *                                 
     */

    /*
     * The `DBAccess` class configuration schema is designed to provide flexible and secure database connection settings.
     * The configuration is typically defined in a configuration file and includes the following key properties:
     *
     * +----------------------+--------------------+---------------------------------------------------------------------------------+
     * | Property             | Configuration Key  | Description                                                                     |
     * +----------------------+--------------------+---------------------------------------------------------------------------------+
     * | DatabaseType         | sType              | Specifies the type of database (SQLServer or ODBC).                             |
     * | Server               | sServer            | The database server name or IP address.                                         |
     * | ServerDSN            | sServerDSN         | The database server DSN (for ODBC connections).                                 |
     * | DatabaseName         | sDatabaseName      | The name of the database.                                                       |
     * | EncryptedUser        | esUser             | The encrypted database user.                                                    |
     * | EncryptedPassword    | esPass             | The encrypted database password.                                                |
     * | ConnectionString     | sConnectionString  | The direct connection string (if provided).                                     |
     * | UseEncryption        | bEncrypt           | Indicates whether the connection should be encrypted.                           |
     * | ConnectionTimeout    | iConnectionTimeout | The connection timeout duration in seconds (defaults to 60 seconds).            |
     * | CommandTimeout       | iCommandTimeout    | The command timeout duration in seconds (defaults to 240 seconds).              |
     * | UseTrustedConnection | bTrustedConnection | Indicates whether the connection uses a trusted connection (defaults to false). |
     * +----------------------+--------------------+---------------------------------------------------------------------------------+
     *
     * The `DBAccess` class uses these configuration settings to establish and manage database connections, execute queries, and handle various database operations.
     * The class also includes methods for encrypting and decrypting sensitive information such as database user credentials.
     */

    /// <summary>
    /// Provides a comprehensive set of methods for interacting with a database.
    /// Supports executing SQL queries, scripts, and retrieving data in various forms such as scalar values, records, and result sets.
    /// Handles database connections, command creation, parameter management, and logging of query execution details.
    /// Includes utility methods for type conversion and timing the execution of database operations.
    /// Designed to work with SQL Server and ODBC databases, and can be configured using a configuration file.
    /// </summary>
    public partial class DBAccess
    {
        /// <summary>
        /// Enum representing the type of database.
        /// </summary>
        public enum DataBaseType
        {
            /// <summary>
            /// Represents a SQL Server database.
            /// </summary>
            SQLServer,

            /// <summary>
            /// Represents an ODBC database.
            /// </summary>
            ODBC,
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DBAccess"/> class using the selected database from the configuration.
        /// </summary>
        public DBAccess() : this("") { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DBAccess"/> class using the specified database section from the configuration.
        /// </summary>
        /// <param name="dbSection">The database section in the configuration.</param>
        /// <exception cref="InvalidOperationException">Thrown when the configuration has not been initialized.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the database section is null or empty.</exception>
        /// <exception cref="ArgumentException">Thrown when the database section contains a colon or does not exist in the configuration, or the database type is not supported.</exception>
        public DBAccess(string dbSection)
        {
            if (string.IsNullOrEmpty(dbSection))
            {
                var _rootOptions = Configuration.GetSection<DatabaseRootOptions>("Data Source");
                dbSection = _rootOptions.SelectedDatabase;
            }
            if (dbSection.Contains(":"))
                throw new ArgumentException("The database section cannot contain a colon.", nameof(dbSection));
            if (Configuration.Root.GetSection(dbSection) == null)
                throw new ArgumentException($"The database section '{dbSection}' does not exist.", nameof(dbSection));

            Options = Configuration.GetSection<DatabaseOptions>(dbSection);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DBAccess"/> class with the specified database options.
        /// </summary>
        /// <param name="options">The configuration options for the database connection. This parameter cannot be <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="options"/> is <see langword="null"/>.</exception>
        public DBAccess(DatabaseOptions options)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Gets the database options for this instance.
        /// </summary>
        public DatabaseOptions Options { get; }

        /// <summary>
        /// Gets the type of the database.
        /// </summary>
        public DataBaseType DbType => Options.DatabaseType;

        /// <summary>
        /// Gets the connection string for the database.
        /// </summary>
        public string ConnectionString => Options.GetConnectionString();

        /// <summary>
        /// Gets the number of records affected by the last executed query.
        /// </summary>
        /// <remarks>
        /// This property is set to -1 if the query did not affect any records.
        /// </remarks>
        public int RecordsAffected { get; private set; }

        /// <summary>
        /// Gets the last exception that occurred during a database operation.
        /// </summary>
        public Exception LastException { get; private set; }
    }
}