using System;

namespace ByteForge.Toolkit
{
    /// <summary>
    /// Represents the configuration options for a database connection.
    /// </summary>
    public class DatabaseOptions
    {
        private readonly Encryptor enc = new Encryptor(13, 16);
        private static readonly object _lock = new object();

        /// <summary>
        /// Gets or sets the type of database (SQLServer or ODBC).
        /// </summary>
        [PropertyName("sType")]
        public DBAccess.DataBaseType DatabaseType { get; set; }

        /// <summary>
        /// Gets or sets the database server name or IP.
        /// </summary>
        [PropertyName("sServer")]
        public string Server { get; set; }

        /// <summary>
        /// Gets or sets the database server DSN (for ODBC connections).
        /// </summary>
        [PropertyName("sServerDSN")]
        public string ServerDSN { get; set; }

        /// <summary>
        /// Gets or sets the database name.
        /// </summary>
        [PropertyName("sDatabaseName")]
        public string DatabaseName { get; set; }

        /// <summary>
        /// Gets or sets the encrypted database user.
        /// </summary>
        [PropertyName("esUser")]
        public string EncryptedUser { get; set; }

        /// <summary>
        /// Gets or sets the encrypted database password.
        /// </summary>
        [PropertyName("esPass")]
        public string EncryptedPassword { get; set; }

        /// <summary>
        /// Gets or sets the direct connection string (if provided).
        /// </summary>
        [PropertyName("sConnectionString")]
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets whether the connection should be encrypted.
        /// </summary>
        [PropertyName("bEncrypt")]
        public bool UseEncryption { get; set; }

        /// <summary>
        /// Gets or sets the connection timeout duration in seconds.
        /// </summary>
        /// <value>
        /// The connection timeout duration in seconds. Defaults to 60 seconds.
        /// </value>
        [PropertyName("iConnectionTimeout")]
        public int ConnectionTimeout { get; set; } = 60;

        /// <summary>
        /// Gets or sets the command timeout duration in seconds.
        /// </summary>
        /// <value>
        /// The command timeout duration in seconds. Defaults to 240 seconds.
        /// </value>
        [PropertyName("iCommandTimeout")]
        public int CommandTimeout { get; set; } = 240;

        /// <summary>
        /// Gets or sets whether the connection uses a trusted connection.
        /// </summary>
        /// <value>
        /// <c>true</c> if the connection uses a trusted connection; otherwise, <c>false</c>. Defaults to <c>false</c>.
        /// </value>
        [PropertyName("bTrustedConnection")]
        public bool UseTrustedConnection { get; set; } = false;

        /// <summary>
        /// Gets the decrypted database user.
        /// </summary>
        [DoNotPersist]
        public string User
        {
            get
            {
                lock (_lock)
                {
                    try { return enc.Decrypt(EncryptedUser); }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to decrypt database user");
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the decrypted database password.
        /// </summary>
        [DoNotPersist]
        public string Password
        {
            get
            {
                lock (_lock)
                {
                    try { return enc.Decrypt(EncryptedPassword); }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to decrypt database password");
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the complete connection string based on the configuration options.
        /// </summary>
        /// <returns>A connection string suitable for database connection.</returns>
        public string GetConnectionString()
        {
            if (!string.IsNullOrEmpty(ConnectionString))
                return ConnectionString;

            try
            {
                switch (DatabaseType)
                {
                    case DBAccess.DataBaseType.SQLServer:
                        return $"Server={Server};" +
                               $"Database={DatabaseName};" +
                               $"User ID={User};" +
                               $"Password={Password};" +
                               $"Encrypt={(UseEncryption ? "Optional" : "false")};" +
                               $"Trusted_Connection={UseTrustedConnection.ToString().ToLowerInvariant()};" +
                               $"Connection Timeout={ConnectionTimeout}";

                    case DBAccess.DataBaseType.ODBC:
                        return $"DSN={ServerDSN};" +
                               $"SERVER={Server};" +
                               $"DATABASE={DatabaseName};" +
                               $"UID={User};" +
                               $"PASSWORD={Password};";

                    default:
                        throw new NotSupportedException($"The database type {DatabaseType} is not supported.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to build connection string");
                throw;
            }
        }
    }

    /// <summary>
    /// Represents the root database configuration options.
    /// </summary>
    public class DatabaseRootOptions
    {
        /// <summary>
        /// Gets or sets the selected database configuration section.
        /// </summary>
        [PropertyName("SelectedDB")]
        public string SelectedDatabase { get; set; }
    }
}