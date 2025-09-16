using System;
using System.ComponentModel;

namespace ByteForge.Toolkit
{
    /*
     *  ___       _        _                   ___       _   _             
     * |   \ __ _| |_ __ _| |__  __ _ ___ ___ / _ \ _ __| |_(_)___ _ _  ___
     * | |) / _` |  _/ _` | '_ \/ _` (_-</ -_) (_) | '_ \  _| / _ \ ' \(_-<
     * |___/\__,_|\__\__,_|_.__/\__,_/__/\___|\___/| .__/\__|_\___/_||_/__/
     *                                             |_|                     
     */
    /// <summary>
    /// Represents the configuration options for a database connection.
    /// </summary>
    /// <remarks>
    /// This class stores all the parameters needed to establish and manage database connections
    /// across different database types (SQL Server and ODBC). It handles secure storage of
    /// credentials through encryption and provides methods to build properly formatted
    /// connection strings based on the configuration.
    /// <para>
    /// The class is typically used with the <see cref="Configuration"/> system to load
    /// database settings from configuration files. Most properties are decorated with
    /// <see cref="ConfigNameAttribute"/> to map between configuration keys and class properties.
    /// </para>
    /// <para>
    /// Sensitive information like database credentials are stored in encrypted form and
    /// only decrypted when needed to build connection strings.
    /// </para>
    /// <example>
    /// <code>
    /// var dbOptions = Configuration.Default.GetSection&lt;DatabaseOptions&gt;("Database");
    /// using (var connection = new SqlConnection(dbOptions.GetConnectionString()))
    /// {
    ///     connection.Open();
    ///     // Execute database operations
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    public class DatabaseOptions
    {
        private static readonly object _lock = new object();

        /// <summary>
        /// Gets or sets the type of database (SQLServer or ODBC).
        /// </summary>
        /// <remarks>
        /// This determines which connection string format will be used when
        /// <see cref="GetConnectionString"/> is called.
        /// </remarks>
        [ConfigName("sType")]
        public DBAccess.DataBaseType DatabaseType { get; set; }

        /// <summary>
        /// Gets or sets the database server name or IP.
        /// </summary>
        /// <remarks>
        /// For SQL Server connections, this is typically a server name, instance name,
        /// or IP address. For example: "localhost", "SQLSERVER01\INSTANCE1", or "192.168.1.100".
        /// </remarks>
        [ConfigName("sServer")]
        public string Server { get; set; }

        /// <summary>
        /// Gets or sets the database server DSN (for ODBC connections).
        /// </summary>
        /// <remarks>
        /// The Data Source Name (DSN) is only used when <see cref="DatabaseType"/> is set to
        /// <see cref="DBAccess.DataBaseType.ODBC"/>. It should reference a system DSN
        /// configured in the ODBC Data Source Administrator.
        /// </remarks>
        [ConfigName("sServerDSN")]
        public string ServerDSN { get; set; }

        /// <summary>
        /// Gets or sets the database name.
        /// </summary>
        /// <remarks>
        /// Specifies the name of the database to connect to on the server.
        /// </remarks>
        [ConfigName("sDatabaseName")]
        public string DatabaseName { get; set; }

        /// <summary>
        /// Gets or sets the encrypted database user.
        /// </summary>
        /// <remarks>
        /// This property stores the encrypted username. To access the decrypted value,
        /// use the <see cref="User"/> property.
        /// <para>
        /// The value is encrypted using the <see cref="Encryptor"/> class with
        /// predefined encryption keys.
        /// </para>
        /// </remarks>
        [ConfigName("esUser")]
        public string EncryptedUser { get; set; }

        /// <summary>
        /// Gets or sets the encrypted database password.
        /// </summary>
        /// <remarks>
        /// This property stores the encrypted password. To access the decrypted value,
        /// use the <see cref="Password"/> property.
        /// <para>
        /// The value is encrypted using the <see cref="Encryptor"/> class with
        /// predefined encryption keys.
        /// </para>
        /// </remarks>
        [ConfigName("esPass")]
        public string EncryptedPassword { get; set; }

        /// <summary>
        /// Gets or sets the direct connection string (if provided).
        /// </summary>
        /// <remarks>
        /// If this property is set, it will be used directly instead of constructing a
        /// connection string from the individual properties. This allows for custom
        /// connection strings that might need additional parameters not exposed by this class.
        /// <para>
        /// Note that when using a direct connection string, credentials may be stored in
        /// plain text, so use this with caution in scenarios requiring high security.
        /// </para>
        /// </remarks>
        [ConfigName("sConnectionString")]
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets whether the connection should be encrypted.
        /// </summary>
        /// <remarks>
        /// For SQL Server connections, determines whether to use TLS/SSL encryption
        /// for the database connection. When true, the "Encrypt=Optional" parameter
        /// is added to the connection string.
        /// <para>
        /// This property affects the transport-level encryption of the database connection
        /// and is separate from the encryption of credentials in configuration storage.
        /// </para>
        /// </remarks>
        [ConfigName("bEncrypt")]
        public bool UseEncryption { get; set; }

        /// <summary>
        /// Gets or sets the connection timeout duration in seconds.
        /// </summary>
        /// <value>
        /// The connection timeout duration in seconds. Defaults to 60 seconds.
        /// </value>
        /// <remarks>
        /// Determines how long to wait for a connection to be established before
        /// timing out. This is particularly important in scenarios where network
        /// connectivity might be unreliable.
        /// </remarks>
        [DefaultValue(10)]
        [ConfigName("iConnectionTimeout")]
        public int ConnectionTimeout { get; set; } = 10;

        /// <summary>
        /// Gets or sets the command timeout duration in seconds.
        /// </summary>
        /// <value>
        /// The command timeout duration in seconds. Defaults to 240 seconds.
        /// </value>
        /// <remarks>
        /// Determines how long to wait for a command (query) to execute before
        /// timing out. This timeout applies to individual SQL commands rather than
        /// the initial connection establishment.
        /// <para>
        /// The default of 240 seconds (4 minutes) is suitable for most operations,
        /// but may need to be increased for long-running reports or data imports.
        /// </para>
        /// </remarks>
        [DefaultValue(240)]
        [ConfigName("iCommandTimeout")]
        public int CommandTimeout { get; set; } = 240;

        /// <summary>
        /// Gets or sets whether the connection uses a trusted connection.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the connection uses a trusted connection; otherwise, <see langword="false" />. Defaults to <see langword="false" />.
        /// </value>
        /// <remarks>
        /// When set to true, Windows Authentication (integrated security) is used
        /// instead of SQL Server Authentication. In this case, the current Windows
        /// identity is used for authentication and the username/password properties
        /// are ignored.
        /// <para>
        /// This is often preferred in intranet scenarios where domain authentication
        /// is available and provides better security than storing credentials.
        /// </para>
        /// </remarks>
        [DefaultValue(false)]
        [ConfigName("bTrustedConnection")]
        public bool UseTrustedConnection { get; set; } = false;

        /// <summary>
        /// Gets or sets whether string values should be automatically trimmed.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if string values should be auto-trimmed; otherwise, <see langword="false" />. Defaults to <see langword="true" />.
        /// </value>
        /// <remarks>
        /// When enabled, string values stored to the database will be automatically trimmed of leading and trailing whitespace.
        /// </remarks>
        [DefaultValue(true)]
        [ConfigName("bAutoTrimStrings")]
        public bool AutoTrimStrings { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether null strings are allowed.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if null strings are allowed; otherwise, <see langword="false" />. Defaults to <see langword="false" />."
        /// </value>
        /// <remarks>
        /// When enabled, string properties can be set to null and will be stored as NULL in the database.
        /// </remarks>
        [DefaultValue(false)]
        [ConfigName("bAllowNullStrings")]
        public bool AllowNullStrings { get; set; } = false;

        /// <summary>
        /// Gets or sets the database user name in a clear format.
        /// </summary>
        /// <remarks>
        /// Accessing this property decrypts the stored encrypted user name. <br />
        /// Setting this property encrypts the provided value before storing it. <br />
        /// Thread safety is ensured by locking during both get and set operations.
        /// </remarks>
        [Ignore]
        public string User
        {
            get
            {
                lock (_lock)
                {
                    try { return Encryptor.Default.Decrypt(EncryptedUser); }
                    catch (Exception ex)
                    {
                        Log.Error("Failed to decrypt database user", ex);
                        throw;
                    }
                }
            }
            set
            {
                lock (_lock)
                {
                    EncryptedUser = Encryptor.Default.Encrypt(value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the password in an encrypted form.
        /// </summary>
        /// <remarks>
        /// Accessing this property decrypts the stored encrypted password. <br />
        /// Setting this property encrypts the provided value before storing it. <br />
        /// Thread safety is ensured by locking during both get and set operations.
        /// </remarks>
        [Ignore]
        public string Password
        {
            get
            {
                lock (_lock)
                {
                    try { return Encryptor.Default.Decrypt(EncryptedPassword); }
                    catch (Exception ex)
                    {
                        Log.Error("Failed to decrypt database password", ex);
                        throw;
                    }
                }
            }
            set
            {
                lock (_lock)
                {
                    EncryptedPassword = Encryptor.Default.Encrypt(value);
                }
            }
        }

        /// <summary>
        /// Gets the complete connection string based on the configuration options.
        /// </summary>
        /// <returns>A connection string suitable for database connection.</returns>
        /// <remarks>
        /// This method generates a properly formatted connection string based on the
        /// database type and configuration settings. It handles the following cases:
        /// <list type="bullet">
        ///   <item>If <see cref="ConnectionString"/> is provided, it's used directly</item>
        ///   <item>For SQL Server, builds a connection string with appropriate parameters</item>
        ///   <item>For ODBC, builds a DSN-based connection string</item>
        /// </list>
        /// <para>
        /// Sensitive information (username and password) is automatically decrypted
        /// during connection string generation.
        /// </para>
        /// </remarks>
        /// <exception cref="NotSupportedException">Thrown when an unsupported database type is specified.</exception>
        public string GetConnectionString()
        {
            if (!string.IsNullOrEmpty(ConnectionString))
                return ConnectionString;

            try
            {
                return DatabaseType switch
                {
                    DBAccess.DataBaseType.SQLServer => $"Server={Server};" +
                                                       $"Database={DatabaseName};" +
                                                       $"User ID={User};" +
                                                       $"Password={Password};" +
                                                       $"Encrypt={(UseEncryption ? "Optional" : "false")};" +
                                                       $"Trusted_Connection={(UseTrustedConnection ? "true" : "false")};" +
                                                       $"Connection Timeout={ConnectionTimeout}",
                    DBAccess.DataBaseType.ODBC => $"DSN={ServerDSN};" +
                                                  $"SERVER={Server};" +
                                                  $"DATABASE={DatabaseName};" +
                                                  $"UID={User};" +
                                                  $"PASSWORD={Password};",
                    _ => throw new NotSupportedException($"The database type {DatabaseType} is not supported."),
                };
            }
            catch (Exception ex)
            {
                Log.Error("Failed to build connection string", ex);
                throw;
            }
        }
    }

    /// <summary>
    /// Represents the root database configuration options.
    /// </summary>
    /// <remarks>
    /// This class is used to determine which database configuration section should be used
    /// when multiple database configurations are defined in the configuration file.
    /// <para>
    /// It typically appears at the root level of configuration and references a section name
    /// that contains the actual database configuration options.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // In configuration file:
    /// // [Data Source]
    /// // SelectedDB = Production
    /// // 
    /// // [Production]
    /// // sType=SQLServer
    /// // sServer=ProdServer
    /// // ...
    /// 
    /// var rootOptions = Configuration.Default.GetSection&lt;DatabaseRootOptions&gt;("Database");
    /// var dbOptions = Configuration.Default.GetSection&lt;DatabaseOptions&gt;(rootOptions.SelectedDatabase);
    /// </code>
    /// </example>
    public class DatabaseRootOptions
    {
        /// <summary>
        /// Gets or sets the selected database configuration section.
        /// </summary>
        /// <remarks>
        /// This property contains the name of the configuration section that holds
        /// the database connection details. The value typically corresponds to an
        /// environment name like "Development", "Testing", or "Production".
        /// </remarks>
        [ConfigName("SelectedDB")]
        public string SelectedDatabase { get; set; }
    }
}