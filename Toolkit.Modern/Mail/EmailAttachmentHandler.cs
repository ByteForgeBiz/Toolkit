using System.IO.Compression;
using System.Net.Mail;

namespace ByteForge.Toolkit.Mail;
/*
 *  ___            _ _   _  _   _           _                  _   _  _              _ _         
 * | __|_ __  __ _(_) | /_\| |_| |_ __ _ __| |_  _ __  ___ _ _| |_| || |__ _ _ _  __| | |___ _ _ 
 * | _|| '  \/ _` | | |/ _ \  _|  _/ _` / _| ' \| '  \/ -_) ' \  _| __ / _` | ' \/ _` | / -_) '_|
 * |___|_|_|_\__,_|_|_/_/ \_\__|\__\__,_\__|_||_|_|_|_\___|_||_\__|_||_\__,_|_||_\__,_|_\___|_|  
 *                                                                                               
 */
/// <summary>
/// Handles email attachments with size restrictions, compression, and splitting capabilities.
/// </summary>
public partial class EmailAttachmentHandler : IDisposable
{
    /// <summary>
    /// Maximum allowed size for an individual file in megabytes.
    /// </summary>
    private const int MaxIndividualFileSizeMB = 5;

    /// <summary>
    /// Maximum allowed total size for all attachments in megabytes.
    /// </summary>
    private const int MaxTotalSizeMB = 20;

    /// <summary>
    /// Path to the temporary directory used for storing compressed files.
    /// </summary>
    private readonly string TempDirectory = Path.Combine(Path.GetTempPath(), "TempEmailAttachments");

    /// <summary>
    /// Maximum allowed size for an individual file in bytes.
    /// </summary>
    private const int MaxIndividualFileSizeBytes = MaxIndividualFileSizeMB * 1024 * 1024;

    /// <summary>
    /// Maximum allowed total size for all attachments in bytes.
    /// </summary>
    private const int MaxTotalSizeBytes = MaxTotalSizeMB * 1024 * 1024;

    private bool _disposed;

    /// <summary>
    /// Processes files for email attachment with size limiting, compression, and splitting if needed.
    /// </summary>
    /// <param name="email">The email message to attach files to.</param>
    /// <param name="filesToAttach">List of file paths to attach.</param>
    /// <param name="addAttachmentSummary">If true, adds a summary of attachments to the email body.</param>
    /// <returns>Result describing the processing outcome.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the email parameter is null.</exception>
    public AttachmentProcessResult ProcessAttachments(MailMessage email, List<string> filesToAttach, bool addAttachmentSummary = true)
    {
        return ProcessAttachments(email, filesToAttach, null, addAttachmentSummary);
    }

    /// <summary>
    /// Processes files for email attachment with size limiting, compression, and splitting if needed.
    /// Supports renaming files in attachments.
    /// </summary>
    /// <param name="email">The email message to attach files to.</param>
    /// <param name="filesToAttach">List of file paths to attach.</param>
    /// <param name="fileNameMap">Optional dictionary mapping file paths to desired attachment names.</param>
    /// <param name="addAttachmentSummary">If true, adds a summary of attachments to the email body.</param>
    /// <returns>Result describing the processing outcome.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the email parameter is null.</exception>
    public AttachmentProcessResult ProcessAttachments(MailMessage email, List<string>? filesToAttach,
        Dictionary<string, string>? fileNameMap = null, bool addAttachmentSummary = true)
    {
        fileNameMap ??= [];
        filesToAttach ??= [];
        var result = new AttachmentProcessResult();

        if (!ValidateInputs(email, filesToAttach))
            return result;

        var fileInfos = GetValidFileInfos(filesToAttach, result);
        var totalSize = fileInfos.Sum(fi => fi.Length);

        if (totalSize <= MaxIndividualFileSizeBytes)
            return AttachFilesDirectly(email, fileInfos, fileNameMap, addAttachmentSummary, result);

        EnsureTempDirectoryExists();

        var timeStamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        var zipFileName = Path.Combine(TempDirectory, $"Attachments_{timeStamp}.zip");

        if (!TryCompressFiles(filesToAttach, zipFileName, fileNameMap, result))
            return result;

        var files = GetCompressedFiles(zipFileName);
        var totalCompressedSize = files.Sum(f => f.Length);

        if (!ValidateCompressedSize(totalCompressedSize, result))
            return result;

        AttachCompressedFiles(email, files, fileNameMap, addAttachmentSummary);

        result.Error = null;
        result.ProcessingMethod = files.Length > 1 ? ProcessingMethod.MultiPart : ProcessingMethod.Compressed;
        return result;
    }

    /// <summary>
    /// Validates the input parameters for attachment processing.
    /// </summary>
    /// <param name="email">The email message to attach files to.</param>
    /// <param name="filesToAttach">List of file paths to attach.</param>
    /// <returns>True if inputs are valid; otherwise, false.</returns>
    private bool ValidateInputs(MailMessage email, List<string> filesToAttach)
    {
        if (filesToAttach == null || filesToAttach.Count == 0)
            return false;

        if (email == null)
            throw new ArgumentNullException(nameof(email));

        return true;
    }

    /// <summary>
    /// Gets valid file information objects for the files to attach.
    /// </summary>
    /// <param name="filesToAttach">List of file paths to attach.</param>
    /// <param name="result">The result object to update with skipped files.</param>
    /// <returns>List of valid <see cref="FileInfo"/> objects.</returns>
    private List<FileInfo> GetValidFileInfos(List<string> filesToAttach, AttachmentProcessResult result)
    {
        var fileInfos = new List<FileInfo>();
        foreach (var file in filesToAttach)
        {
            if (!File.Exists(file))
            {
                result.SkippedFiles.Add(new SkippedFile { FilePath = file, Reason = "File not found" });
                continue;
            }
            fileInfos.Add(new FileInfo(file));
        }
        return fileInfos;
    }

    /// <summary>
    /// Attaches files directly to the email message.
    /// </summary>
    /// <param name="email">The email message to attach files to.</param>
    /// <param name="fileInfos">List of valid <see cref="FileInfo"/> objects.</param>
    /// <param name="fileNameMap">Optional dictionary mapping file paths to desired attachment names.</param>
    /// <param name="addAttachmentSummary">If true, adds a summary of attachments to the email body.</param>
    /// <param name="result">The result object to update.</param>
    /// <returns>The updated <see cref="AttachmentProcessResult"/>.</returns>
    private AttachmentProcessResult AttachFilesDirectly(
        MailMessage email,
        List<FileInfo> fileInfos,
        Dictionary<string, string> fileNameMap,
        bool addAttachmentSummary,
        AttachmentProcessResult result)
    {
        var attachedFiles = new List<FileInfo>();
        foreach (var fileInfo in fileInfos)
        {
            var attachment = new Attachment(fileInfo.FullName)
            {
                Name = GetDisplayName(fileInfo.FullName, fileNameMap)
            };
            email.Attachments.Add(attachment);
            attachedFiles.Add(fileInfo);
        }

        if (addAttachmentSummary)
            AddDirectAttachmentSummary(email, attachedFiles, fileNameMap);

        result.ProcessingMethod = ProcessingMethod.DirectAttachment;
        return result;
    }

    /// <summary>
    /// Ensures the temporary directory exists for storing compressed files.
    /// </summary>
    private void EnsureTempDirectoryExists()
    {
        if (!Directory.Exists(TempDirectory))
            Directory.CreateDirectory(TempDirectory);
    }

    /// <summary>
    /// Attempts to compress files into a zip archive.
    /// </summary>
    /// <param name="filesToAttach">List of file paths to attach.</param>
    /// <param name="zipFileName">The output zip file path.</param>
    /// <param name="fileNameMap">Optional dictionary mapping file paths to desired names in the archive.</param>
    /// <param name="result">The result object to update with errors.</param>
    /// <returns>True if compression succeeded; otherwise, false.</returns>
    private bool TryCompressFiles(
        List<string> filesToAttach,
        string zipFileName,
        Dictionary<string, string> fileNameMap,
        AttachmentProcessResult result)
    {
        try
        {
            CompressFiles(filesToAttach.Where(f => File.Exists(f)).ToList(), zipFileName, fileNameMap);
            result.ProcessingMethod = ProcessingMethod.Compressed;
            return true;
        }
        catch (Exception ex)
        {
            result.Error = $"Failed to compress files: {ex.Message}";
            result.ProcessingMethod = ProcessingMethod.Failed;
            return false;
        }
    }

    /// <summary>
    /// Gets the compressed files from the temporary directory.
    /// </summary>
    /// <param name="zipFileName">The base zip file name.</param>
    /// <returns>Array of <see cref="FileInfo"/> objects for the compressed files.</returns>
    private FileInfo[] GetCompressedFiles(string zipFileName)
    {
        var zipSearch = Path.GetFileName(Path.ChangeExtension(zipFileName, ".z*"));
        var di = new DirectoryInfo(TempDirectory);
        return di.GetFiles(zipSearch);
    }

    /// <summary>
    /// Validates the total size of compressed files.
    /// </summary>
    /// <param name="totalCompressedSize">Total size of compressed files in bytes.</param>
    /// <param name="result">The result object to update with errors.</param>
    /// 
    /// <returns>True if compressed size is valid; otherwise, false.</returns>
    private bool ValidateCompressedSize(long totalCompressedSize, AttachmentProcessResult result)
    {
        if (totalCompressedSize == 0)
        {
            // Probably will never happen, but just in case
            result.Error = "Compression resulted in zero-size files.";
            result.ProcessingMethod = ProcessingMethod.Failed;
            return false;
        }
        else if (totalCompressedSize > MaxTotalSizeBytes)
        {
            // Even after compression, files are too large
            result.Error = "Files too large to attach even after compression.";
            result.ProcessingMethod = ProcessingMethod.Failed;
            return false;
        }
        return true;
    }

    /// <summary>
    /// Attaches compressed files to the email message.
    /// </summary>
    /// <param name="email">The email message to attach files to.</param>
    /// <param name="files">Array of compressed <see cref="FileInfo"/> objects.</param>
    /// <param name="fileNameMap">Optional dictionary mapping file paths to desired attachment names.</param>
    /// <param name="addAttachmentSummary">If true, adds a summary of attachments to the email body.</param>
    private void AttachCompressedFiles(
        MailMessage email,
        FileInfo[] files,
        Dictionary<string, string> fileNameMap,
        bool addAttachmentSummary)
    {
        foreach (var file in files)
        {
            var attachment = new TempFileAttachment(file.FullName);
            if (fileNameMap != null && fileNameMap.Count == 1)
                attachment.Name = Path.ChangeExtension(fileNameMap.Values.First(), Path.GetExtension(file.Name));
            email.Attachments.Add(attachment);
        }

        if (addAttachmentSummary)
            AddCompressedAttachmentSummary(email, files.ToList(), email.Attachments[email.Attachments.Count - 1], fileNameMap);
    }

    /// <summary>
    /// Compresses a list of files into a single zip archive.
    /// </summary>
    /// <param name="files">The list of files to compress.</param>
    /// <param name="outputZipFile">The output zip file path.</param>
    /// <param name="fileNameMap">Optional dictionary mapping file paths to desired names in the archive.</param>
    private void CompressFiles(List<string> files, string outputZipFile, Dictionary<string, string>? fileNameMap = null)
    {
#pragma warning disable IDE0063
        /*
         * VS says to use simple 'using' statement.
         * But for some reason, that doesn't work with ZipArchive.
         */
        fileNameMap ??= new Dictionary<string, string>();
        using (var fileStream = new FileStream(outputZipFile, FileMode.Create))
        {
            using (var zip = new ZipArchive(fileStream, ZipArchiveMode.Create))
            {
                foreach (var file in files)
                    if (File.Exists(file))
                    {
                        var entryName = GetDisplayName(file, fileNameMap);
                        _ = zip.CreateEntryFromFile(file, entryName, CompressionLevel.Optimal);
                    }

            }
        }
#pragma warning restore IDE0063
    }

    /// <summary>
    /// Gets the display name for a file, using the mapped name if available.
    /// </summary>
    /// <param name="filePath">The full file path.</param>
    /// <param name="fileNameMap">Optional dictionary mapping file paths to desired names.</param>
    /// <returns>The display name to use for the file.</returns>
    private string GetDisplayName(string filePath, Dictionary<string, string> fileNameMap)
    {
        var key = Path.GetFileName(filePath);
        if (fileNameMap != null)
        {
            if (fileNameMap.ContainsKey(key))
                return fileNameMap[key];
            if (fileNameMap.ContainsKey(filePath))
                return fileNameMap[filePath];
        }


        return Path.GetFileName(filePath);
    }

    /// <summary>
    /// Adds a summary of direct file attachments to the email body.
    /// </summary>
    /// <param name="email">The email message to add the summary to.</param>
    /// <param name="files">The list of attached files.</param>
    /// <param name="fileNameMap">Optional dictionary mapping file paths to desired names.</param>
    private void AddDirectAttachmentSummary(MailMessage email, List<FileInfo> files, Dictionary<string, string>? fileNameMap = null)
    {
        if (files == null || files.Count == 0)
            return;

        fileNameMap ??= [];
        var summary = new System.Text.StringBuilder();
        summary.AppendLine();
        summary.AppendLine("----");
        summary.AppendLine("Attached:");

        foreach (var file in files)
        {
            var sizeStr = FormatFileSize(file.Length);
            var displayName = GetDisplayName(file.FullName, fileNameMap);
            summary.AppendLine($"   - {displayName} ({sizeStr})");
        }

        email.Body += summary.ToString();
    }

    /// <summary>
    /// Adds a summary of compressed file attachments to the email body.
    /// </summary>
    /// <param name="email">The email message to add the summary to.</param>
    /// <param name="originalFiles">The list of original files.</param>
    /// <param name="zip">The zip attachment.</param>
    /// <param name="fileNameMap">Optional dictionary mapping file paths to desired names.</param>
    private void AddCompressedAttachmentSummary(MailMessage email, List<FileInfo> originalFiles, Attachment zip, Dictionary<string, string>? fileNameMap = null)
    {
        if (originalFiles == null || originalFiles.Count == 0)
            return;

        fileNameMap ??= [];
        var zipSizeStr = FormatFileSize(zip.ContentStream.Length);

        var summary = new System.Text.StringBuilder();
        summary.AppendLine();
        summary.AppendLine("----");
        summary.AppendLine("Attached:");
        summary.AppendLine($"   - {zip.Name} ({zipSizeStr})");

        foreach (var file in originalFiles)
        {
            var displayName = GetDisplayName(file.FullName, fileNameMap);
            summary.AppendLine($"       - {displayName}");
        }

        email.Body += summary.ToString();
    }

    /// <summary>
    /// Formats a file size in bytes to a human-readable string.
    /// </summary>
    /// <param name="bytes">The file size in bytes.</param>
    /// <returns>A human-readable string representing the file size.</returns>
    private string FormatFileSize(long bytes)
    {
        var counter = 0;
        decimal number = bytes;

        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }

        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        return $"{number:n1} {suffixes[counter]}";
    }

    /// <summary>
    /// Releases the unmanaged resources used by the object and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">A value indicating whether to release both managed and unmanaged resources (<see langword="true"/>) or only unmanaged resources (<see langword="false"/>).</param>
    /// <remarks>
    /// This method is called by the public <see cref="Dispose()"/> method and the finalizer.<br/>
    /// When <paramref name="disposing"/> is <see langword="true"/>, this method releases all resources held 
    /// by managed objects that the object references. Override this method to release resources specific  
    /// to your derived class.
    /// </remarks>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            // Cleanup temporary directory
            try
            {
                if (Directory.Exists(TempDirectory))
                    Directory.Delete(TempDirectory, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
            _disposed = true;
        }
    }

    /// <summary>
    /// Releases the resources used by the current instance of the class.
    /// </summary>
    /// <remarks>
    /// This method should be called when the instance is no longer needed to ensure that all
    /// unmanaged resources are properly released.<br/>
    /// It suppresses finalization to optimize garbage collection.</remarks>
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
