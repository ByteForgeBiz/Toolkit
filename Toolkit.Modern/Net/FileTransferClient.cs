#nullable disable

using ByteForge.WinSCP;

namespace ByteForge.Toolkit.Net;
/// <summary>
/// Universal file transfer client supporting FTP, FTPS, and SFTP protocols.
/// </summary>
public class FileTransferClient : IDisposable
{
    private readonly FileTransferConfig _config;
    private Session _session;
    private readonly SemaphoreSlim _sessionLock = new SemaphoreSlim(1, 1);
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance with the specified configuration.
    /// </summary>
    /// <param name="config">The connection configuration</param>
    public FileTransferClient(FileTransferConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        ValidateConfig();
    }

    /// <summary>
    /// Establishes a connection to the file transfer server.
    /// </summary>
    /// <returns>True if connection is successful</returns>
    public bool Connect()
    {
        try
        {
            if (_session?.Opened == true)
                return true;

            var sessionOptions = CreateSessionOptions();

            _session = new Session();
            _session.Open(sessionOptions);

            return _session.Opened;
        }
        catch (Exception ex)
        {
            throw new FileTransferException($"Failed to connect to {_config.Protocol} server: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Asynchronously establishes a connection to the file transfer server.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>True if connection is successful</returns>
    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await _sessionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_session?.Opened == true)
                    return true;

                var sessionOptions = CreateSessionOptions();

                return await Task.Run(() =>
                {
                    _session = new Session();
                    _session.Open(sessionOptions);
                    return _session.Opened;
                }, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _sessionLock.Release();
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new FileTransferException($"Failed to connect to {_config.Protocol} server: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Disconnects from the server if connected.
    /// </summary>
    public void Disconnect()
    {
        try
        {
            if (_session?.Opened == true)
                _session.Close();
        }
        catch (Exception ex)
        {
            throw new FileTransferException($"Error disconnecting from server: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Asynchronously disconnects from the server if connected.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _sessionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_session?.Opened == true)
                {
                    await Task.Run(() => _session.Close(), cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                _sessionLock.Release();
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new FileTransferException($"Error disconnecting from server: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Uploads a file to the server.
    /// </summary>
    /// <param name="localFilePath">Path to the local file</param>
    /// <param name="remoteFilePath">Path where the file will be stored on the server</param>
    /// <param name="overwrite">Whether to overwrite existing files</param>
    /// <returns>True if upload is successful</returns>
    public bool UploadFile(string localFilePath, string remoteFilePath, bool overwrite = false)
    {
        ValidateConnection();
        ValidateLocalFile(localFilePath);

        try
        {
            var transferOptions = new TransferOptions
            {
                TransferMode = _config.Protocol == TransferProtocol.FTP ? TransferMode.Binary : TransferMode.Binary,
                OverwriteMode = overwrite ? OverwriteMode.Overwrite : OverwriteMode.Resume
            };

            var transferResult = _session.PutFiles(localFilePath, remoteFilePath, false, transferOptions);
            transferResult.Check();

            return transferResult.IsSuccess;
        }
        catch (Exception ex)
        {
            throw new FileTransferException($"Failed to upload file '{localFilePath}' to '{remoteFilePath}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Asynchronously uploads a file to the server.
    /// </summary>
    /// <param name="localFilePath">Path to the local file</param>
    /// <param name="remoteFilePath">Path where the file will be stored on the server</param>
    /// <param name="overwrite">Whether to overwrite existing files</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>True if upload is successful</returns>
    public async Task<bool> UploadFileAsync(string localFilePath, string remoteFilePath, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        await ValidateConnectionAsync(cancellationToken).ConfigureAwait(false);
        ValidateLocalFile(localFilePath);

        try
        {
            var transferOptions = new TransferOptions
            {
                TransferMode = _config.Protocol == TransferProtocol.FTP ? TransferMode.Binary : TransferMode.Binary,
                OverwriteMode = overwrite ? OverwriteMode.Overwrite : OverwriteMode.Resume
            };

            return await Task.Run(() =>
            {
                var transferResult = _session.PutFiles(localFilePath, remoteFilePath, false, transferOptions);
                transferResult.Check();
                return transferResult.IsSuccess;
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new FileTransferException($"Failed to upload file '{localFilePath}' to '{remoteFilePath}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Uploads multiple files to the server concurrently.
    /// </summary>
    /// <param name="transferItems">Collection of file transfer items</param>
    /// <param name="maxConcurrent">Maximum number of concurrent transfers (default: 5)</param>
    /// <param name="progress">Optional progress reporting callback</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>Collection of transfer results</returns>
    public async Task<IEnumerable<FileTransferResult>> UploadFilesAsync(
        IEnumerable<FileTransferItem> transferItems, 
        int maxConcurrent = 5, 
        IProgress<FileTransferProgress> progress = null, 
        CancellationToken cancellationToken = default)
    {
        if (transferItems == null)
            throw new ArgumentNullException(nameof(transferItems));

        var items = transferItems.ToList();
        if (items.Count == 0)
            return Enumerable.Empty<FileTransferResult>();

        await ValidateConnectionAsync(cancellationToken).ConfigureAwait(false);

        var results = new List<FileTransferResult>();
        var semaphore = new SemaphoreSlim(maxConcurrent, maxConcurrent);
        var tasks = new List<Task<FileTransferResult>>();
        var processedCount = 0;
        var totalCount = items.Count;

        foreach (var item in items)
        {
            // Validate file exists before queueing
            if (!File.Exists(item.LocalPath))
            {
                results.Add(new FileTransferResult
                {
                    LocalPath = item.LocalPath,
                    RemotePath = item.RemotePath,
                    Success = false,
                    ErrorMessage = $"Local file not found: {item.LocalPath}"
                });
                continue;
            }

            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            
            var task = Task.Run(async () =>
            {
                try
                {
                    var result = new FileTransferResult
                    {
                        LocalPath = item.LocalPath,
                        RemotePath = item.RemotePath
                    };

                    try
                    {
                        result.Success = await UploadFileAsync(item.LocalPath, item.RemotePath, item.Overwrite, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        result.Success = false;
                        result.ErrorMessage = ex.Message;
                    }

                    Interlocked.Increment(ref processedCount);
                    progress?.Report(new FileTransferProgress 
                    { 
                        ProcessedCount = processedCount, 
                        TotalCount = totalCount,
                        PercentComplete = (int)((double)processedCount / totalCount * 100),
                        CurrentItem = item
                    });

                    return result;
                }
                finally
                {
                    semaphore.Release();
                }
            }, cancellationToken);

            tasks.Add(task);
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
        results.AddRange(tasks.Select(t => t.Result));
        
        return results;
    }

    /// <summary>
    /// Downloads a file from the server.
    /// </summary>
    /// <param name="remoteFilePath">Path to the file on the server</param>
    /// <param name="localFilePath">Local path where the file will be saved</param>
    /// <param name="overwrite">Whether to overwrite existing local files</param>
    /// <returns>True if download is successful</returns>
    public bool DownloadFile(string remoteFilePath, string localFilePath, bool overwrite = false)
    {
        ValidateConnection();

        try
        {
            var transferOptions = new TransferOptions
            {
                TransferMode = TransferMode.Binary,
                OverwriteMode = overwrite ? OverwriteMode.Overwrite : OverwriteMode.Resume
            };

            var transferResult = _session.GetFiles(remoteFilePath, localFilePath, false, transferOptions);
            transferResult.Check();

            return transferResult.IsSuccess;
        }
        catch (Exception ex)
        {
            throw new FileTransferException($"Failed to download file '{remoteFilePath}' to '{localFilePath}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Asynchronously downloads a file from the server.
    /// </summary>
    /// <param name="remoteFilePath">Path to the file on the server</param>
    /// <param name="localFilePath">Local path where the file will be saved</param>
    /// <param name="overwrite">Whether to overwrite existing local files</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>True if download is successful</returns>
    public async Task<bool> DownloadFileAsync(string remoteFilePath, string localFilePath, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        await ValidateConnectionAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var transferOptions = new TransferOptions
            {
                TransferMode = TransferMode.Binary,
                OverwriteMode = overwrite ? OverwriteMode.Overwrite : OverwriteMode.Resume
            };

            return await Task.Run(() =>
            {
                var transferResult = _session.GetFiles(remoteFilePath, localFilePath, false, transferOptions);
                transferResult.Check();
                return transferResult.IsSuccess;
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new FileTransferException($"Failed to download file '{remoteFilePath}' to '{localFilePath}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Downloads multiple files from the server concurrently.
    /// </summary>
    /// <param name="transferItems">Collection of file transfer items</param>
    /// <param name="maxConcurrent">Maximum number of concurrent transfers (default: 5)</param>
    /// <param name="progress">Optional progress reporting callback</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>Collection of transfer results</returns>
    public async Task<IEnumerable<FileTransferResult>> DownloadFilesAsync(
        IEnumerable<FileTransferItem> transferItems, 
        int maxConcurrent = 5, 
        IProgress<FileTransferProgress> progress = null, 
        CancellationToken cancellationToken = default)
    {
        if (transferItems == null)
            throw new ArgumentNullException(nameof(transferItems));

        var items = transferItems.ToList();
        if (items.Count == 0)
            return Enumerable.Empty<FileTransferResult>();

        await ValidateConnectionAsync(cancellationToken).ConfigureAwait(false);

        var results = new List<FileTransferResult>();
        var semaphore = new SemaphoreSlim(maxConcurrent, maxConcurrent);
        var tasks = new List<Task<FileTransferResult>>();
        var processedCount = 0;
        var totalCount = items.Count;

        foreach (var item in items)
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            
            var task = Task.Run(async () =>
            {
                try
                {
                    var result = new FileTransferResult
                    {
                        LocalPath = item.LocalPath,
                        RemotePath = item.RemotePath
                    };

                    try
                    {
                        // Create directory if it doesn't exist
                        var directory = Path.GetDirectoryName(item.LocalPath);
                        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }

                        result.Success = await DownloadFileAsync(item.RemotePath, item.LocalPath, item.Overwrite, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        result.Success = false;
                        result.ErrorMessage = ex.Message;
                    }

                    Interlocked.Increment(ref processedCount);
                    progress?.Report(new FileTransferProgress 
                    { 
                        ProcessedCount = processedCount, 
                        TotalCount = totalCount,
                        PercentComplete = (int)((double)processedCount / totalCount * 100),
                        CurrentItem = item
                    });

                    return result;
                }
                finally
                {
                    semaphore.Release();
                }
            }, cancellationToken);

            tasks.Add(task);
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
        results.AddRange(tasks.Select(t => t.Result));
        
        return results;
    }

    /// <summary>
    /// Lists files and directories in the specified remote directory.
    /// </summary>
    /// <param name="remotePath">Remote directory path</param>
    /// <returns>Collection of remote file information</returns>
    public IEnumerable<RemoteFileInfo> ListDirectory(string remotePath = "/")
    {
        ValidateConnection();

        try
        {
            var directoryInfo = _session.ListDirectory(remotePath);
            return directoryInfo.Files
                .Where(f => f.Name != "." && f.Name != "..")
                .Select(f => new RemoteFileInfo
                {
                    Name = f.Name,
                    FullPath = f.FullName,
                    Size = f.Length,
                    LastModified = f.LastWriteTime,
                    IsDirectory = f.IsDirectory,
                    Permissions = f.FilePermissions?.Text
                });
        }
        catch (Exception ex)
        {
            throw new FileTransferException($"Failed to list directory '{remotePath}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Asynchronously lists files and directories in the specified remote directory.
    /// </summary>
    /// <param name="remotePath">Remote directory path</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>Collection of remote file information</returns>
    public async Task<IEnumerable<RemoteFileInfo>> ListDirectoryAsync(string remotePath = "/", CancellationToken cancellationToken = default)
    {
        await ValidateConnectionAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            return await Task.Run(() =>
            {
                var directoryInfo = _session.ListDirectory(remotePath);
                return directoryInfo.Files
                    .Where(f => f.Name != "." && f.Name != "..")
                    .Select(f => new RemoteFileInfo
                    {
                        Name = f.Name,
                        FullPath = f.FullName,
                        Size = f.Length,
                        LastModified = f.LastWriteTime,
                        IsDirectory = f.IsDirectory,
                        Permissions = f.FilePermissions?.Text
                    });
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new FileTransferException($"Failed to list directory '{remotePath}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Checks if a file exists on the remote server.
    /// </summary>
    /// <param name="remoteFilePath">Path to check</param>
    /// <returns>True if file exists</returns>
    public bool FileExists(string remoteFilePath)
    {
        ValidateConnection();

        try
        {
            return _session.FileExists(remoteFilePath);
        }
        catch (Exception ex)
        {
            throw new FileTransferException($"Failed to check if file exists '{remoteFilePath}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Asynchronously checks if a file exists on the remote server.
    /// </summary>
    /// <param name="remoteFilePath">Path to check</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>True if file exists</returns>
    public async Task<bool> FileExistsAsync(string remoteFilePath, CancellationToken cancellationToken = default)
    {
        await ValidateConnectionAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            return await Task.Run(() => _session.FileExists(remoteFilePath), cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new FileTransferException($"Failed to check if file exists '{remoteFilePath}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Deletes a file from the remote server.
    /// </summary>
    /// <param name="remoteFilePath">Path to the file to delete</param>
    /// <returns>True if deletion is successful</returns>
    public bool DeleteFile(string remoteFilePath)
    {
        ValidateConnection();

        try
        {
            _session.RemoveFiles(remoteFilePath);
            return true;
        }
        catch (Exception ex)
        {
            throw new FileTransferException($"Failed to delete file '{remoteFilePath}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Asynchronously deletes a file from the remote server.
    /// </summary>
    /// <param name="remoteFilePath">Path to the file to delete</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>True if deletion is successful</returns>
    public async Task<bool> DeleteFileAsync(string remoteFilePath, CancellationToken cancellationToken = default)
    {
        await ValidateConnectionAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            return await Task.Run(() =>
            {
                _session.RemoveFiles(remoteFilePath);
                return true;
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new FileTransferException($"Failed to delete file '{remoteFilePath}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Deletes multiple files from the remote server concurrently.
    /// </summary>
    /// <param name="remoteFilePaths">Collection of paths to files to delete</param>
    /// <param name="maxConcurrent">Maximum number of concurrent operations (default: 5)</param>
    /// <param name="progress">Optional progress reporting callback</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>Collection of deletion results</returns>
    public async Task<IEnumerable<FileOperationResult>> DeleteFilesAsync(
        IEnumerable<string> remoteFilePaths, 
        int maxConcurrent = 5, 
        IProgress<FileOperationProgress> progress = null, 
        CancellationToken cancellationToken = default)
    {
        if (remoteFilePaths == null)
            throw new ArgumentNullException(nameof(remoteFilePaths));

        var paths = remoteFilePaths.ToList();
        if (paths.Count == 0)
            return Enumerable.Empty<FileOperationResult>();

        await ValidateConnectionAsync(cancellationToken).ConfigureAwait(false);

        var results = new List<FileOperationResult>();
        var semaphore = new SemaphoreSlim(maxConcurrent, maxConcurrent);
        var tasks = new List<Task<FileOperationResult>>();
        var processedCount = 0;
        var totalCount = paths.Count;

        foreach (var path in paths)
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            
            var task = Task.Run(async () =>
            {
                try
                {
                    var result = new FileOperationResult
                    {
                        Path = path
                    };

                    try
                    {
                        result.Success = await DeleteFileAsync(path, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        result.Success = false;
                        result.ErrorMessage = ex.Message;
                    }

                    Interlocked.Increment(ref processedCount);
                    progress?.Report(new FileOperationProgress 
                    { 
                        ProcessedCount = processedCount, 
                        TotalCount = totalCount,
                        PercentComplete = (int)((double)processedCount / totalCount * 100),
                        CurrentPath = path
                    });

                    return result;
                }
                finally
                {
                    semaphore.Release();
                }
            }, cancellationToken);

            tasks.Add(task);
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
        results.AddRange(tasks.Select(t => t.Result));
        
        return results;
    }

    /// <summary>
    /// Creates a directory on the remote server.
    /// </summary>
    /// <param name="remoteDirectoryPath">Path of the directory to create</param>
    /// <returns>True if creation is successful</returns>
    public bool CreateDirectory(string remoteDirectoryPath)
    {
        ValidateConnection();

        try
        {
            _session.CreateDirectory(remoteDirectoryPath);
            return true;
        }
        catch (Exception ex)
        {
            throw new FileTransferException($"Failed to create directory '{remoteDirectoryPath}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Asynchronously creates a directory on the remote server.
    /// </summary>
    /// <param name="remoteDirectoryPath">Path of the directory to create</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>True if creation is successful</returns>
    public async Task<bool> CreateDirectoryAsync(string remoteDirectoryPath, CancellationToken cancellationToken = default)
    {
        await ValidateConnectionAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            return await Task.Run(() =>
            {
                _session.CreateDirectory(remoteDirectoryPath);
                return true;
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new FileTransferException($"Failed to create directory '{remoteDirectoryPath}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Creates session options based on the current configuration.
    /// </summary>
    /// <returns>Configured session options for the WinSCP connection</returns>
    private SessionOptions CreateSessionOptions()
    {
        var sessionOptions = new SessionOptions
        {
            HostName = _config.HostName!,
            UserName = _config.UserName!,
            Password = _config.Password!,
            PortNumber = _config.Port,
            Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds)
        };

        switch (_config.Protocol)
        {
            case TransferProtocol.FTP:
                sessionOptions.Protocol = Protocol.Ftp;
                break;

            case TransferProtocol.FTPS_Explicit:
                sessionOptions.Protocol = Protocol.Ftp;
                sessionOptions.FtpSecure = FtpSecure.Explicit;
                sessionOptions.GiveUpSecurityAndAcceptAnyTlsHostCertificate = _config.AcceptAnyCertificate;
                break;

            case TransferProtocol.FTPS_Implicit:
                sessionOptions.Protocol = Protocol.Ftp;
                sessionOptions.FtpSecure = FtpSecure.Implicit;
                sessionOptions.GiveUpSecurityAndAcceptAnyTlsHostCertificate = _config.AcceptAnyCertificate;
                break;

            case TransferProtocol.SFTP:
                sessionOptions.Protocol = Protocol.Sftp;
                sessionOptions.SshHostKeyPolicy = _config.AcceptAnyHostKey ? SshHostKeyPolicy.GiveUpSecurityAndAcceptAny : SshHostKeyPolicy.AcceptNew;
                if (!string.IsNullOrEmpty(_config.SshPrivateKeyPath))
                {
                    sessionOptions.SshPrivateKeyPath = _config.SshPrivateKeyPath!;
                    sessionOptions.Password = null;
                }
                break;

            default:
                throw new ArgumentException($"Unsupported protocol: {_config.Protocol}");
        }

        return sessionOptions;
    }

    /// <summary>
    /// Validates the configuration settings before using them.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when configuration is invalid</exception>
    private void ValidateConfig()
    {
        if (string.IsNullOrWhiteSpace(_config.HostName))
            throw new ArgumentException("HostName cannot be null or empty");

        if (string.IsNullOrWhiteSpace(_config.UserName))
            throw new ArgumentException("UserName cannot be null or empty");

        if (_config.Protocol == TransferProtocol.SFTP)
        {
            if (string.IsNullOrWhiteSpace(_config.Password) && string.IsNullOrWhiteSpace(_config.SshPrivateKeyPath))
                throw new ArgumentException("For SFTP, either Password or SshPrivateKeyPath must be provided");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(_config.Password))
                throw new ArgumentException("Password is required for FTP/FTPS connections");
        }

        if (_config.Port <= 0 || _config.Port > 65535)
            throw new ArgumentException("Port must be between 1 and 65535");
    }

    /// <summary>
    /// Validates that an active connection exists.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no active connection exists</exception>
    private void ValidateConnection()
    {
        if (_session?.Opened != true)
            throw new InvalidOperationException($"Not connected to {_config.Protocol} server. Call Connect() first.");
    }

    /// <summary>
    /// Asynchronously validates that an active connection exists.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="InvalidOperationException">Thrown when no active connection exists</exception>
    private async Task ValidateConnectionAsync(CancellationToken cancellationToken)
    {
        if (_session?.Opened != true)
            throw new InvalidOperationException($"Not connected to {_config.Protocol} server. Call ConnectAsync() first.");

        // Just a small delay to respect the cancellation token and to make the compiler happy 
        await Task.Run(() => { }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Validates that a local file exists.
    /// </summary>
    /// <param name="localFilePath">Path to the local file</param>
    /// <exception cref="FileNotFoundException">Thrown when the local file does not exist</exception>
    private void ValidateLocalFile(string localFilePath)
    {
        if (string.IsNullOrWhiteSpace(localFilePath))
            throw new ArgumentException("Local file path cannot be null or empty");

        if (!File.Exists(localFilePath))
            throw new FileNotFoundException($"Local file not found: {localFilePath}");
    }

    /// <summary>
    /// Disposes the file transfer client and releases all resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Core dispose method, disposing managed and unmanaged resources.
    /// </summary>
    /// <param name="disposing">True if called from Dispose()</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed resources
                Disconnect();
                _sessionLock.Dispose();
            }

            // Dispose unmanaged resources if any

            _disposed = true;
        }
    }
}
