namespace ByteForge.Toolkit.Tests.Helpers
{
    /// <summary>
    /// Helper class for managing temporary files and directories in tests.
    /// </summary>
    public static class TempFileHelper
    {
        private static readonly List<string> _tempFiles = new List<string>();
        private static readonly List<string> _tempDirectories = new List<string>();

        /// <summary>
        /// Creates a temporary file with the specified content.
        /// </summary>
        /// <param name="content">The content to write to the file.</param>
        /// <param name="extension">The file extension (default: .tmp).</param>
        /// <returns>The path to the created temporary file.</returns>
        public static string CreateTempFile(string content, string extension = ".tmp")
        {
            var tempFile = Path.ChangeExtension(Path.GetTempFileName(), extension);
            File.WriteAllText(tempFile, content);
            _tempFiles.Add(tempFile);
            return tempFile;
        }

        /// <summary>
        /// Creates a temporary directory.
        /// </summary>
        /// <returns>The path to the created temporary directory.</returns>
        public static string CreateTempDirectory()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            _tempDirectories.Add(tempDir);
            return tempDir;
        }

        /// <summary>
        /// Creates a temporary file with binary data.
        /// </summary>
        /// <param name="data">The binary data to write.</param>
        /// <param name="extension">The file extension.</param>
        /// <returns>The path to the created temporary file.</returns>
        public static string CreateTempFile(byte[] data, string extension = ".dat")
        {
            var tempFile = Path.ChangeExtension(Path.GetTempFileName(), extension);
            File.WriteAllBytes(tempFile, data);
            _tempFiles.Add(tempFile);
            return tempFile;
        }

        /// <summary>
        /// Creates a temporary CSV file with the specified content.
        /// </summary>
        /// <param name="csvContent">The CSV content.</param>
        /// <returns>The path to the created CSV file.</returns>
        public static string CreateTempCsvFile(string csvContent)
        {
            return CreateTempFile(csvContent, ".csv");
        }

        /// <summary>
        /// Creates a temporary INI file with the specified content.
        /// </summary>
        /// <param name="iniContent">The INI content.</param>
        /// <returns>The path to the created INI file.</returns>
        public static string CreateTempIniFile(string iniContent)
        {
            return CreateTempFile(iniContent, ".ini");
        }

        /// <summary>
        /// Cleans up all temporary files and directories created by this helper.
        /// </summary>
        public static void CleanupTempFiles()
        {
            // Clean up files
            foreach (var file in _tempFiles)
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
            _tempFiles.Clear();

            // Clean up directories
            foreach (var dir in _tempDirectories)
            {
                try
                {
                    if (Directory.Exists(dir))
                        Directory.Delete(dir, recursive: true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
            _tempDirectories.Clear();
        }

        /// <summary>
        /// Gets a unique temporary file path without creating the file.
        /// </summary>
        /// <param name="extension">The file extension.</param>
        /// <returns>A unique temporary file path.</returns>
        public static string GetTempFilePath(string extension = ".tmp")
        {
            var tempFile = Path.ChangeExtension(Path.GetTempFileName(), extension);
            File.Delete(tempFile); // Delete the created file, we just want the path
            _tempFiles.Add(tempFile); // Track for cleanup
            return tempFile;
        }
    }
}