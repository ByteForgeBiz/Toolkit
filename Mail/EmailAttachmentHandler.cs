using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Mail;

namespace ByteForge.Toolkit
{
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
    public partial class EmailAttachmentHandler
    {
        private const long MaxIndividualFileSizeMB = 5;
        private const long MaxTotalSizeMB = 20;
        private const string TempDirectory = "TempEmailAttachments";

        // Convert MB to bytes for comparisons
        private const long MaxIndividualFileSizeBytes = MaxIndividualFileSizeMB * 1024 * 1024;
        private const long MaxTotalSizeBytes = MaxTotalSizeMB * 1024 * 1024;

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
        public AttachmentProcessResult ProcessAttachments(MailMessage email, List<string> filesToAttach,
            Dictionary<string, string> fileNameMap = null, bool addAttachmentSummary = true)
        {
            var result = new AttachmentProcessResult();

            // Validate inputs
            if (filesToAttach == null || filesToAttach.Count == 0)
                return result;

            if (email == null)
                throw new ArgumentNullException(nameof(email));

            // Check total size of all files
            long totalSize = 0;
            var fileInfos = new List<FileInfo>();

            foreach (var file in filesToAttach)
            {
                if (!File.Exists(file))
                {
                    result.SkippedFiles.Add(new SkippedFile { FilePath = file, Reason = "File not found" });
                    continue;
                }

                var fileInfo = new FileInfo(file);
                fileInfos.Add(fileInfo);
                totalSize += fileInfo.Length;
            }

            // If total size is under limit, attach files directly
            if (totalSize <= MaxIndividualFileSizeBytes)
            {
                var attachedFiles = new List<FileInfo>();
                foreach (var file in filesToAttach)
                {
                    if (File.Exists(file))
                    {
                        var attachment = new Attachment(file)
                        {
                            Name = GetDisplayName(file, fileNameMap)
                        };

                        email.Attachments.Add(attachment);
                        attachedFiles.Add(new FileInfo(file));
                    }
                }

                if (addAttachmentSummary)
                    AddDirectAttachmentSummary(email, attachedFiles, fileNameMap);

                result.ProcessingMethod = ProcessingMethod.DirectAttachment;
                return result;
            }

            // Create temp directory for processing if it doesn't exist
            if (!Directory.Exists(TempDirectory))
                Directory.CreateDirectory(TempDirectory);

            var timeStamp = DateTime.Now.ToString("yyyyMMddHHmmss");

            // If we need compression, try to compress all files first to see if it's enough
            var zipFileName = Path.Combine(TempDirectory, $"Attachments_{timeStamp}.zip");
            try
            {
                CompressFiles(filesToAttach.Where(f => File.Exists(f)).ToList(), zipFileName, fileNameMap);
                result.TempFilesCreated.Add(zipFileName);
                result.ProcessingMethod = ProcessingMethod.Compressed;
            }
            catch (Exception ex)
            {
                result.Error = $"Failed to compress files: {ex.Message}";
                return result;
            }

            // Check if compressed file is still too large
            var zipFileInfo = new FileInfo(zipFileName);

            if (zipFileInfo.Length <= MaxIndividualFileSizeBytes)
            {
                // Compressed file is under the limit, attach it

                var zipAttachment = new Attachment(zipFileName);
                if (filesToAttach.Count == 1 && fileNameMap.Count == 1)
                    zipAttachment.Name = Path.ChangeExtension(GetDisplayName(filesToAttach[0], fileNameMap), ".zip");

                email.Attachments.Add(zipAttachment);

                if (addAttachmentSummary)
                    AddCompressedAttachmentSummary(email, fileInfos, zipAttachment, fileNameMap);
            }
            else if (zipFileInfo.Length <= MaxTotalSizeBytes)
            {
                // Need to create multi-part archives
                result.ProcessingMethod = ProcessingMethod.CompressedAndSplit;

                // Delete the single zip file since we'll create multiple ones
                File.Delete(zipFileName);
                result.TempFilesCreated.Remove(zipFileName);

                // Calculate optimal number of parts for balanced splitting
                var optimalPartCount = (int)Math.Ceiling((double)totalSize / MaxIndividualFileSizeBytes);
                if (optimalPartCount > MaxTotalSizeMB / MaxIndividualFileSizeMB)
                    optimalPartCount = (int)(MaxTotalSizeMB / MaxIndividualFileSizeMB);

                // Create multi-part zip files
                var multiPartZips = CreateMultiPartZipArchives(fileInfos, optimalPartCount, timeStamp, fileNameMap, out var buckets);

                foreach (var partZip in multiPartZips)
                {
                    email.Attachments.Add(new Attachment(partZip));
                    result.TempFilesCreated.Add(partZip);
                }

                if (addAttachmentSummary)
                    AddMultiPartAttachmentSummary(email, fileInfos, multiPartZips, buckets, fileNameMap);

                result.PartDistribution = buckets.Select(b => new PartInfo
                {
                    PartNumber = b.Index + 1,
                    FileCount = b.Files.Count,
                    Files = b.Files.Select(f => GetDisplayName(f.FullName, fileNameMap)).ToList()
                }).ToList();
            }
            else
            {
                // Even after compression, total size exceeds max allowed
                result.Error = "Files too large to attach even after compression and splitting.";
                result.ProcessingMethod = ProcessingMethod.Failed;

                // Clean up the zip file
                if (File.Exists(zipFileName))
                    File.Delete(zipFileName);

                result.TempFilesCreated.Remove(zipFileName);
            }

            return result;
        }

        /// <summary>
        /// Compresses a list of files into a single zip archive.
        /// </summary>
        /// <param name="files">The list of files to compress.</param>
        /// <param name="outputZipFile">The output zip file path.</param>
        /// <param name="fileNameMap">Optional dictionary mapping file paths to desired names in the archive.</param>
        private void CompressFiles(List<string> files, string outputZipFile, Dictionary<string, string> fileNameMap = null)
        {
            using var zipArchive = ZipFile.Open(outputZipFile, ZipArchiveMode.Create);
            foreach (var file in files)
            {
                if (File.Exists(file))
                {
                    var entryName = GetDisplayName(file, fileNameMap);
                    zipArchive.CreateEntryFromFile(file, entryName);
                }
            }
        }

        /// <summary>
        /// Creates multiple ZIP files with the contents distributed evenly.
        /// Each ZIP is a valid archive with a subset of the files.
        /// </summary>
        /// <param name="files">The list of files to distribute.</param>
        /// <param name="partCount">The number of parts to create.</param>
        /// <param name="timeStamp">The timestamp to use in the file names.</param>
        /// <param name="fileNameMap">Optional dictionary mapping file paths to desired names in the archive.</param>
        /// <param name="buckets">The output list of file buckets.</param>
        /// <returns>The list of created zip file paths.</returns>
        private List<string> CreateMultiPartZipArchives(List<FileInfo> files, int partCount, string timeStamp,
            Dictionary<string, string> fileNameMap, out List<FileBucket> buckets)
        {
            var zipFiles = new List<string>();
            buckets = new List<FileBucket>();

            if (files.Count == 0 || partCount <= 0)
                return zipFiles;

            // Sort files by size (descending) for better distribution
            var sortedFiles = files.OrderByDescending(f => f.Length).ToList();

            // Create buckets for each part
            buckets = new List<FileBucket>();
            for (var i = 0; i < partCount; i++)
                buckets.Add(new FileBucket { Index = i });

            // Distribute files using a greedy approach (place largest files first)
            foreach (var file in sortedFiles)
            {
                // Find the bucket with the smallest current size
                var smallestBucket = buckets.OrderBy(b => b.TotalSize).First();
                smallestBucket.Files.Add(file);
                smallestBucket.TotalSize += file.Length;
            }

            // Create a zip file for each bucket
            for (var i = 0; i < buckets.Count; i++)
            {
                var bucket = buckets[i];
                if (bucket.Files.Count == 0)
                    continue;

                var zipName = Path.Combine(TempDirectory, $"Attachments_{timeStamp}_Part{i + 1}of{partCount}.zip");

                using (var zipArchive = ZipFile.Open(zipName, ZipArchiveMode.Create))
                {
                    foreach (var file in bucket.Files)
                    {
                        var entryName = GetDisplayName(file.FullName, fileNameMap);
                        zipArchive.CreateEntryFromFile(file.FullName, entryName);
                    }
                }

                zipFiles.Add(zipName);
            }

            return zipFiles;
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
            if (fileNameMap != null && fileNameMap.ContainsKey(key))
                return fileNameMap[key];

            return Path.GetFileName(filePath);
        }

        /// <summary>
        /// Cleans up temporary files created during processing.
        /// </summary>
        /// <param name="result">The result of the attachment processing operation.</param>
        public void CleanupTempFiles(AttachmentProcessResult result)
        {
            if (result?.TempFilesCreated != null)
            {
                foreach (var file in result.TempFilesCreated)
                {
                    try
                    {
                        if (File.Exists(file))
                            File.Delete(file);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
            }
        }

        /// <summary>
        /// Adds a summary of direct file attachments to the email body.
        /// </summary>
        /// <param name="email">The email message to add the summary to.</param>
        /// <param name="files">The list of attached files.</param>
        /// <param name="fileNameMap">Optional dictionary mapping file paths to desired names.</param>
        private void AddDirectAttachmentSummary(MailMessage email, List<FileInfo> files, Dictionary<string, string> fileNameMap = null)
        {
            if (files == null || files.Count == 0)
                return;

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
        private void AddCompressedAttachmentSummary(MailMessage email, List<FileInfo> originalFiles, Attachment zip, Dictionary<string, string> fileNameMap = null)
        {
            if (originalFiles == null || originalFiles.Count == 0)
                return;

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
        /// Adds a summary of multi-part attachments to the email body.
        /// </summary>
        /// <param name="email">The email message to add the summary to.</param>
        /// <param name="originalFiles">The list of original files.</param>
        /// <param name="zipParts">The list of zip part file paths.</param>
        /// <param name="buckets">The list of file buckets.</param>
        /// <param name="fileNameMap">Optional dictionary mapping file paths to desired names.</param>
        private void AddMultiPartAttachmentSummary(MailMessage email, List<FileInfo> originalFiles,
                                                  List<string> zipParts, List<FileBucket> buckets, Dictionary<string, string> fileNameMap = null)
        {
            if (originalFiles is null) throw new ArgumentNullException(nameof(originalFiles));
            if (zipParts == null || zipParts.Count == 0)
                return;

            var summary = new System.Text.StringBuilder();
            summary.AppendLine();
            summary.AppendLine("----");
            summary.AppendLine("Attached:");

            for (var i = 0; i < zipParts.Count; i++)
            {
                var zipInfo = new FileInfo(zipParts[i]);
                var zipSizeStr = FormatFileSize(zipInfo.Length);
                summary.AppendLine($"   - {Path.GetFileName(zipParts[i])} ({zipSizeStr})");

                var bucket = buckets.FirstOrDefault(b => b.Index == i);
                if (bucket != null)
                {
                    foreach (var file in bucket.Files)
                    {
                        var displayName = GetDisplayName(file.FullName, fileNameMap);
                        summary.AppendLine($"       - {displayName}");
                    }
                }
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
    }
}