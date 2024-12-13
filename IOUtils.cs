using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ByteForge.Toolkit;

/// <summary>
/// Provides utility methods for IO operations.
/// </summary>
public class IOUtils
{
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
