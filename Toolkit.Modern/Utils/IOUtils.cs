using ByteForge.Toolkit.Logging;
using System.Runtime.InteropServices;
using System.Text;

namespace ByteForge.Toolkit.Utils;
/*
 *  ___ ___  _   _ _   _ _    
 * |_ _/ _ \| | | | |_(_) |___
 *  | | (_) | |_| |  _| | (_-<
 * |___\___/ \___/ \__|_|_/__/
 *                            
 */
/// <summary>
/// Provides utility methods for IO operations.
/// </summary>
public static class IOUtils
{
    /// <summary>
    /// Gets the files from the specified path that match the search pattern.
    /// </summary>
    /// <param name="path">The directory path to search in.</param>
    /// <param name="searchPattern">The search pattern to match against the names of files in the path. Multiple patterns can be separated by a semicolon (;).</param>
    /// <param name="searchOption">Specifies whether to search the current directory, or the current directory and all subdirectories. The default is <see cref="SearchOption.TopDirectoryOnly"/>.</param>
    /// <returns>An array of the full names (including paths) for the files in the specified directory that match the search pattern.</returns>
    public static string[] GetFiles(string path, string searchPattern, SearchOption searchOption = SearchOption.TopDirectoryOnly)
    {
        var patterns = searchPattern.Split(';', '|');
        return patterns.SelectMany(p => Directory.GetFiles(path, p, searchOption)).ToArray();
    }

    /// <summary>
    /// Retrieves an array of <see cref="FileInfo"/> objects that match the specified search pattern in the given directory.
    /// </summary>
    /// <param name="path">The path to the directory to search. This cannot be null or empty.</param>
    /// <param name="searchPattern">A string containing one or more search patterns, separated by ';' or '|', 
    /// to match against the names of files in the directory. 
    /// Wildcards such as '*' and '?' are supported.</param>
    /// <param name="searchOption">Specifies whether to search only the current directory or all subdirectories. 
    /// The default is <see cref="SearchOption.TopDirectoryOnly"/>.</param>
    /// <returns>
    /// An array of <see cref="FileInfo"/> objects representing the files that match the specified search pattern.
    /// If no files match, an empty array is returned.
    /// </returns>
    /// <remarks>
    /// This method supports multiple search patterns separated by ';' or '|'. For example, a
    /// <paramref name="searchPattern"/> of "*.txt;*.csv" will match all files with a ".txt" or ".csv"
    /// extension.
    /// </remarks>
    public static FileInfo[] GetFileInfos(string path, string searchPattern, SearchOption searchOption = SearchOption.TopDirectoryOnly)
    {
        var patterns = searchPattern.Split(';', '|');
        return patterns.SelectMany(p => new DirectoryInfo(path).GetFiles(p, searchOption)).ToArray();
    }

    /// <summary>
    /// Gets the universal path for the specified path.
    /// </summary>
    /// <param name="path">The path to convert to a universal path.</param>
    /// <returns>The universal path.</returns>
    /// <exception cref="Exception">Thrown when there is an error resolving the path.</exception>
    public static string GetUniversalPath(string path)
    {
        try
        {
            // First normalize the path
            var fullPath = Path.GetFullPath(path);

            // Get the root of the path (e.g., "C:", "D:", etc.)
            var root = Path.GetPathRoot(fullPath);
            if (string.IsNullOrEmpty(root))
                return fullPath;

            // If it's not a drive letter (e.g., already a UNC), return as is
            if (!root.Contains(":"))
                return fullPath;

            // Check if it's a network drive
            var driveLetter = root.TrimEnd('\\');
            var remotePath = new StringBuilder(256);
            var length = remotePath.Capacity;

            var result = WNetGetConnection(driveLetter, remotePath, ref length);

            if (result == 0) // SUCCESS - it's a network drive
            {
                // Replace the drive letter portion with the UNC path
                return Path.Combine(remotePath.ToString(), fullPath.Substring(root.Length).TrimStart('\\'));
            }

            // Not a network drive, return the full local path
            return fullPath;
        }
        catch (Exception ex)
        {
            Log.Error($"Error resolving path: {ex.Message}", ex);
            throw;
        }
    }

    /// <summary>
    /// Retrieves the UNC (Universal Naming Convention) path for a mapped network drive.
    /// </summary>
    /// <param name="localName">The local name of the drive (e.g., "C:").</param>
    /// <param name="remoteName">The StringBuilder to receive the UNC path.</param>
    /// <param name="length">The length of the remoteName buffer.</param>
    /// <returns>0 if the function succeeds, otherwise an error code.</returns>
    [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
    private static extern int WNetGetConnection([MarshalAs(UnmanagedType.LPTStr)] string localName, [MarshalAs(UnmanagedType.LPTStr)] StringBuilder remoteName, ref int length);
}
