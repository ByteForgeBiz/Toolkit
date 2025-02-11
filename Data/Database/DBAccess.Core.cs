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
    /// <summary>
    /// Provides access to the database configuration.
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

        private readonly DatabaseOptions _options;
        private static readonly DatabaseRootOptions _rootOptions = Configuration.GetSection<DatabaseRootOptions>("Data Source");

        /// <summary>
        /// Initializes a new instance of the <see cref="DBAccess"/> class using the selected database from the configuration.
        /// </summary>
        public DBAccess() : this(_rootOptions.SelectedDatabase) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DBAccess"/> class using the specified database section from the configuration.
        /// </summary>
        /// <param name="dbSection">The database section in the configuration.</param>
        /// <exception cref="InvalidOperationException">Thrown when the configuration has not been initialized.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the database section is null or empty.</exception>
        /// <exception cref="ArgumentException">Thrown when the database section contains a colon or does not exist in the configuration.</exception>
        public DBAccess(string dbSection)
        {
            if (Configuration.Root == null)
                throw new InvalidOperationException("The configuration has not been initialized.");
            if (string.IsNullOrEmpty(dbSection))
                throw new ArgumentNullException(nameof(dbSection), "The database section is required.");
            if (dbSection.Contains(":"))
                throw new ArgumentException("The database section should not contain a colon.", nameof(dbSection));
            if (Configuration.Root.GetSection(dbSection) == null)
                throw new ArgumentException($"The database section '{dbSection}' does not exist.", nameof(dbSection));
            if (!Enum.TryParse<DataBaseType>(Configuration.Root[$"{dbSection}:sType"], out _))
                throw new ArgumentException($"The database type '{Configuration.Root[$"{dbSection}:sType"]}' is not supported. Check the configuration file.", nameof(dbSection));

            _options = Configuration.GetSection<DatabaseOptions>(dbSection);
            if (_options == null)
                throw new ArgumentException($"The database section '{dbSection}' does not exist.", nameof(dbSection));
        }

        /// <summary>
        /// Gets the type of the database.
        /// </summary>
        public DataBaseType DbType => _options.DatabaseType;

        /// <summary>  
        /// Gets a value indicating whether the connection is encrypted.  
        /// </summary>  
        public bool EncryptedConnection => _options.UseEncryption;

        /// <summary>
        /// Gets the database server IP.
        /// </summary>
        public string DataBaseServer => _options.Server;

        /// <summary>
        /// Gets the database server DSN.
        /// </summary>
        public string DataBaseServerDSN => _options.ServerDSN;

        /// <summary>
        /// Gets or sets the database name.
        /// </summary>
        public string DataBaseName
        {
            get => _options.DatabaseName;
            set { _options.DatabaseName = value; }
        }

        /// <summary>
        /// Gets the database user.
        /// </summary>
        public string DataBaseUser => _options.User;

        /// <summary>
        /// Gets the database password.
        /// </summary>
        public string DataBasePassword => _options.Password;

        /// <summary>
        /// Gets the connection string for the database.
        /// </summary>
        public string ConnectionString => _options.GetConnectionString();

        /// <summary>
        /// Gets the number of records affected by the last executed query.
        /// </summary>
        /// <remarks>This property is set to -1 if the query did not affected any records.</remarks>
        public int RecordsAffected { get; private set; }

        /// <summary>
        /// Gets the last exception that occurred during a database operation.
        /// </summary>
        public Exception LastException { get; private set; }
    }
}