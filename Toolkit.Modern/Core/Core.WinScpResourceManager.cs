using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;

namespace ByteForge.Toolkit;
public static partial class Core
{
    /// <summary>
    /// Manages extraction, validation (via SHA-256 checksum) and cleanup of embedded WinSCP resources.
    /// </summary>
    /// <remarks>
    /// This class is internal to the <see cref="Core"/> logic and not intended for external consumption.
    /// It compares the checksum of existing files to the embedded resource to avoid unnecessary writes.
    /// </remarks>
    private class WinScpResourceManager
    {
        private const string TOOLKIT = "ByteForge.Toolkit";
        private const string WINSCP_EXE = "WinSCP.exe";
        private const string WINSCP_EXE_RESOURCE = TOOLKIT + ".Dependencies." + WINSCP_EXE;

        private readonly string _executionPath;
        private readonly Assembly _assembly;

        /// <summary>
        /// Initializes a new instance of the <see cref="WinScpResourceManager"/> class.
        /// Determines the execution directory and the assembly containing embedded resources.
        /// </summary>
        public WinScpResourceManager()
        {
            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly() ?? Assembly.GetExecutingAssembly();
            _executionPath = Path.GetDirectoryName(assembly.Location)!;
            _assembly = Assembly.GetExecutingAssembly();
        }

        /// <summary>
        /// Ensures both WinSCP related files exist and are up-to-date, extracting when necessary.
        /// </summary>
        public void EnsureWinScpFilesAvailable()
        {
            EnsureFileAvailable(WINSCP_EXE_RESOURCE, WINSCP_EXE);
        }

        /// <summary>
        /// Validates (via checksum) whether a target file exists and matches the embedded resource;
        /// extracts the resource if validation fails.
        /// </summary>
        /// <param name="resourceName">Fully qualified embedded resource name.</param>
        /// <param name="fileName">Target output file name.</param>
        private void EnsureFileAvailable(string resourceName, string fileName)
        {
            var targetPath = Path.Combine(_executionPath, fileName);

            // Check if file exists and has correct checksum
            if (File.Exists(targetPath) && IsFileUpToDate(resourceName, targetPath))
                return;

            // Extract the file from resources
            ExtractResourceToFile(resourceName, targetPath);
        }

        /// <summary>
        /// Determines if an existing file matches the embedded resource by comparing SHA-256 checksums.
        /// </summary>
        /// <param name="resourceName">Embedded resource name.</param>
        /// <param name="filePath">Path to the existing file on disk.</param>
        /// <returns><c>true</c> if the file matches; otherwise <c>false</c>.</returns>
        /// <remarks>
        /// Any exception during comparison (e.g. missing resource, IO issues) results in a <c>false</c> return.
        /// </remarks>
        private bool IsFileUpToDate(string resourceName, string filePath)
        {
            try
            {
                var resourceChecksum = CalculateResourceChecksum(resourceName);
                var fileChecksum = CalculateFileChecksum(filePath);

                return CompareChecksums(resourceChecksum, fileChecksum);
            }
            catch
            {
                // If we can't calculate checksums, assume file needs updating
                return false;
            }
        }

        /// <summary>
        /// Computes the SHA-256 checksum of the embedded resource.
        /// </summary>
        /// <param name="resourceName">Name of the embedded resource.</param>
        /// <returns>Byte array containing the checksum.</returns>
        /// <exception cref="FileNotFoundException">Thrown when the embedded resource cannot be found.</exception>
        private byte[] CalculateResourceChecksum(string resourceName)
        {
            using var resourceStream = _assembly.GetManifestResourceStream(resourceName);
            return resourceStream == null
                ? throw new FileNotFoundException($"Embedded resource '{resourceName}' not found.")
                : CalculateChecksum(resourceStream);
        }

        /// <summary>
        /// Computes the SHA-256 checksum of a file located on disk.
        /// </summary>
        /// <param name="filePath">Path to the file to hash.</param>
        /// <returns>Byte array containing the checksum.</returns>
        private static byte[] CalculateFileChecksum(string filePath)
        {
            using var fileStream = File.OpenRead(filePath);
            return CalculateChecksum(fileStream);
        }

        /// <summary>
        /// Computes the SHA-256 hash for the provided stream.
        /// </summary>
        /// <param name="stream">Input stream to hash. Stream position should be at the beginning.</param>
        /// <returns>Byte array containing the SHA-256 hash.</returns>
        private static byte[] CalculateChecksum(Stream stream)
        {
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(stream);
        }

        /// <summary>
        /// Compares two checksum byte arrays for equality.
        /// </summary>
        /// <param name="checksum1">First checksum.</param>
        /// <param name="checksum2">Second checksum.</param>
        /// <returns><c>true</c> if lengths match and all bytes are equal; otherwise <c>false</c>.</returns>
        private static bool CompareChecksums(byte[] checksum1, byte[] checksum2)
        {
            if (checksum1.Length != checksum2.Length)
                return false;

            for (var i = 0; i < checksum1.Length; i++)
            {
                if (checksum1[i] != checksum2[i])
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Extracts an embedded resource to a specified file path using a safe temp file + move pattern.
        /// </summary>
        /// <param name="resourceName">Embedded resource to extract.</param>
        /// <param name="targetPath">Destination path on disk.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when extraction fails or the resource cannot be found.
        /// </exception>
        private void ExtractResourceToFile(string resourceName, string targetPath)
        {
            try
            {
                using var resourceStream = _assembly.GetManifestResourceStream(resourceName) ??
                    throw new FileNotFoundException($"Embedded resource '{resourceName}' not found.");

                // Ensure directory exists
                var directory = Path.GetDirectoryName(targetPath)!;
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                // Write to temporary file first, then move to final location
                var tempPath = targetPath + ".tmp";

                using (var fileStream = File.Create(tempPath))
                {
                    resourceStream.CopyTo(fileStream);
                }

                // Move temporary file to final location
                if (File.Exists(targetPath))
                    File.Delete(targetPath);

                File.Move(tempPath, targetPath);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to extract resource '{resourceName}' to '{targetPath}'", ex);
            }
        }

        /// <summary>
        /// Determines whether both WinSCP related files exist and match the embedded resources.
        /// </summary>
        /// <returns><c>true</c> if both files are present and up-to-date; otherwise <c>false</c>.</returns>
        public bool AreWinScpFilesAvailable()
        {
            var winScpExePath = Path.Combine(_executionPath, WINSCP_EXE);

            return File.Exists(winScpExePath) &&
                   IsFileUpToDate(WINSCP_EXE_RESOURCE, winScpExePath);
        }

        /// <summary>
        /// Attempts to delete the extracted WinSCP files, ignoring any failures.
        /// </summary>
        public void CleanupWinScpFiles()
        {
            var winScpExePath = Path.Combine(_executionPath, WINSCP_EXE);

            try
            {
                if (File.Exists(winScpExePath))
                    File.Delete(winScpExePath);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
