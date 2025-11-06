namespace ByteForge.Toolkit;
/// <summary>
/// Provides core helper functionality for the toolkit.
/// </summary>
/// <remarks>
/// Currently exposes APIs for ensuring that embedded WinSCP distribution files
/// (WinSCP.exe and WinSCPnet.dll) are extracted and available alongside the executing assembly.
/// </remarks>
public static partial class Core
{
#if NETFRAMEWORK
    /// <summary>
    /// Ensures that the WinSCP executable and its .NET wrapper library are present
    /// on disk next to the entry assembly, extracting them from embedded resources if required.
    /// </summary>
    /// <remarks>
    /// Extraction occurs only when a file is missing or its checksum differs from the embedded resource.
    /// </remarks>
    public static void EnsureWinSCP()
    {
        var mgr = new WinScpResourceManager();
        mgr.EnsureWinScpFilesAvailable();
    }
#endif
}
