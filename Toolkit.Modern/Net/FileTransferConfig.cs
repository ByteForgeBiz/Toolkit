namespace ByteForge.Toolkit.Net
{
    /// <summary>
    /// Configuration settings for file transfer connection.
    /// </summary>
    public class FileTransferConfig
    {
        /// <summary>
        /// Gets or sets the hostname or IP address of the server.
        /// </summary>
        public string? HostName { get; set; }

        /// <summary>
        /// Gets or sets the username for authentication.
        /// </summary>
        public string? UserName { get; set; }

        /// <summary>
        /// Gets or sets the password for authentication.
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// Gets or sets the path to the SSH private key file (for SFTP only).
        /// </summary>
        public string? SshPrivateKeyPath { get; set; }

        /// <summary>
        /// Gets or sets the transfer protocol to use.
        /// </summary>
        public TransferProtocol Protocol { get; set; } = TransferProtocol.SFTP;

        /// <summary>
        /// Gets or sets the port number for the connection.
        /// Default is 22 for SFTP, 21 for FTP/FTPS.
        /// </summary>
        public int Port { get; set; } = 22;

        /// <summary>
        /// Gets or sets the connection timeout in seconds.
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Gets or sets whether to accept any SSH host key without verification (for SFTP).
        /// </summary>
        /// <remarks>
        /// Setting this to true reduces security but simplifies connections.
        /// </remarks>
        public bool AcceptAnyHostKey { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to accept any SSL/TLS certificate without verification (for FTPS).
        /// </summary>
        /// <remarks>
        /// Setting this to true reduces security but simplifies connections.
        /// </remarks>
        public bool AcceptAnyCertificate { get; set; } = false;

        /// <summary>
        /// Creates a configuration for FTP connection.
        /// </summary>
        /// <param name="hostName">The hostname or IP address of the FTP server</param>
        /// <param name="userName">The username for authentication</param>
        /// <param name="password">The password for authentication</param>
        /// <param name="port">The port number (default: 21)</param>
        /// <returns>A configured FileTransferConfig instance for FTP</returns>
        public static FileTransferConfig CreateFtpConfig(string hostName, string userName, string password, int port = 21)
        {
            return new FileTransferConfig
            {
                HostName = hostName,
                UserName = userName,
                Password = password,
                Protocol = TransferProtocol.FTP,
                Port = port
            };
        }

        /// <summary>
        /// Creates a configuration for FTPS connection.
        /// </summary>
        /// <param name="hostName">The hostname or IP address of the FTPS server</param>
        /// <param name="userName">The username for authentication</param>
        /// <param name="password">The password for authentication</param>
        /// <param name="useImplicitTls">Whether to use implicit TLS (true) or explicit TLS (false)</param>
        /// <param name="port">The port number (defaults to 990 for implicit TLS, 21 for explicit TLS if not specified)</param>
        /// <returns>A configured FileTransferConfig instance for FTPS</returns>
        public static FileTransferConfig CreateFtpsConfig(string hostName, string userName, string password,
            bool useImplicitTls = false, int port = 0)
        {
            return new FileTransferConfig
            {
                HostName = hostName,
                UserName = userName,
                Password = password,
                Protocol = useImplicitTls ? TransferProtocol.FTPS_Implicit : TransferProtocol.FTPS_Explicit,
                Port = port > 0 ? port : (useImplicitTls ? 990 : 21),
                AcceptAnyCertificate = true
            };
        }

        /// <summary>
        /// Creates a configuration for SFTP connection.
        /// </summary>
        /// <param name="hostName">The hostname or IP address of the SFTP server</param>
        /// <param name="userName">The username for authentication</param>
        /// <param name="password">The password for authentication</param>
        /// <param name="port">The port number (default: 22)</param>
        /// <returns>A configured FileTransferConfig instance for SFTP</returns>
        public static FileTransferConfig CreateSftpConfig(string hostName, string userName, string password, int port = 22)
        {
            return new FileTransferConfig
            {
                HostName = hostName,
                UserName = userName,
                Password = password,
                Protocol = TransferProtocol.SFTP,
                Port = port,
                AcceptAnyHostKey = true
            };
        }
    }
}
