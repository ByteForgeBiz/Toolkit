using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace ByteForge.WinSCP;

/// <summary>
/// Exposes P/Invoke declarations for native Windows API functions used by the WinSCP wrapper.
/// </summary>
internal static class UnsafeNativeMethods
{
	/// <summary>
	/// The Win32 error code returned when a named object already exists.
	/// </summary>
	public const int ERROR_ALREADY_EXISTS = 183;

	/// <summary>
	/// Creates or opens a named or unnamed file mapping object for the specified file.
	/// </summary>
	/// <param name="hFile">A handle to the file from which to create the mapping.</param>
	/// <param name="lpAttributes">A pointer to a security attributes structure, or <see cref="IntPtr.Zero"/> for a default descriptor.</param>
	/// <param name="fProtect">The page protection to apply to mapped pages.</param>
	/// <param name="dwMaximumSizeHigh">The high-order <c>DWORD</c> of the maximum size of the file mapping object.</param>
	/// <param name="dwMaximumSizeLow">The low-order <c>DWORD</c> of the maximum size of the file mapping object.</param>
	/// <param name="lpName">The name of the file mapping object, or <see langword="null"/> for an unnamed object.</param>
	/// <returns>A <see cref="SafeFileHandle"/> for the file mapping object, or an invalid handle on failure.</returns>
	[DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
	public static extern SafeFileHandle CreateFileMapping(SafeFileHandle hFile, IntPtr lpAttributes, FileMapProtection fProtect, int dwMaximumSizeHigh, int dwMaximumSizeLow, string lpName);

	/// <summary>
	/// Maps a view of a file mapping into the address space of the calling process.
	/// </summary>
	/// <param name="handle">A handle to the file mapping object.</param>
	/// <param name="dwDesiredAccess">The type of access to a file mapping object.</param>
	/// <param name="dwFileOffsetHigh">The high-order <c>DWORD</c> of the file offset where the view begins.</param>
	/// <param name="dwFileOffsetLow">The low-order <c>DWORD</c> of the file offset where the view begins.</param>
	/// <param name="dwNumberOfBytesToMap">The number of bytes of a file mapping to map to the view.</param>
	/// <returns>A pointer to the starting address of the mapped view, or <see cref="IntPtr.Zero"/> on failure.</returns>
	[DllImport("kernel32", ExactSpelling = true, SetLastError = true)]
	public static extern IntPtr MapViewOfFile(SafeFileHandle handle, FileMapAccess dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, UIntPtr dwNumberOfBytesToMap);

	/// <summary>
	/// Unmaps a mapped view of a file from the calling process's address space.
	/// </summary>
	/// <param name="lpBaseAddress">A pointer to the base address of the mapped view to unmap.</param>
	/// <returns><see langword="true"/> if the function succeeds; otherwise, <see langword="false"/>.</returns>
	[DllImport("kernel32", ExactSpelling = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

	/// <summary>
	/// Closes an open object handle.
	/// </summary>
	/// <param name="hObject">A valid handle to an open object.</param>
	/// <returns>A non-zero value if the function succeeds; otherwise, zero.</returns>
	[DllImport("kernel32", CharSet = CharSet.Auto, ExactSpelling = true, SetLastError = true)]
	public static extern int CloseHandle(IntPtr hObject);

	/// <summary>
	/// Creates or opens a job object.
	/// </summary>
	/// <param name="a">A pointer to a security attributes structure, or <see cref="IntPtr.Zero"/> for a default descriptor.</param>
	/// <param name="lpName">The name of the job object, or <see langword="null"/> for an unnamed object.</param>
	/// <returns>A handle to the job object, or <see cref="IntPtr.Zero"/> on failure.</returns>
	[DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
	public static extern IntPtr CreateJobObject(IntPtr a, string lpName);

	/// <summary>
	/// Sets limits and other job information for the specified job object.
	/// </summary>
	/// <param name="hJob">A handle to the job object.</param>
	/// <param name="infoType">The information class to set.</param>
	/// <param name="lpJobObjectInfo">A pointer to the structure that contains the information to set.</param>
	/// <param name="cbJobObjectInfoLength">The size of the structure in bytes.</param>
	/// <returns><see langword="true"/> if the function succeeds; otherwise, <see langword="false"/>.</returns>
	[DllImport("kernel32", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool SetInformationJobObject(IntPtr hJob, JobObjectInfoType infoType, IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength);

	/// <summary>
	/// Returns a handle to the window station associated with the calling process.
	/// </summary>
	/// <returns>A handle to the current window station, or <see cref="IntPtr.Zero"/> on failure.</returns>
	[DllImport("user32.dll", SetLastError = true)]
	public static extern IntPtr GetProcessWindowStation();

	/// <summary>
	/// Returns a handle to the desktop assigned to the specified thread.
	/// </summary>
	/// <param name="dwThreadId">The thread identifier.</param>
	/// <returns>A handle to the desktop, or <see cref="IntPtr.Zero"/> on failure.</returns>
	[DllImport("user32.dll", SetLastError = true)]
	public static extern IntPtr GetThreadDesktop(int dwThreadId);

	/// <summary>
	/// Returns the thread identifier of the calling thread.
	/// </summary>
	/// <returns>The thread identifier of the calling thread.</returns>
	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern int GetCurrentThreadId();

	/// <summary>
	/// Retrieves the type and data for the specified registry value.
	/// </summary>
	/// <param name="hkey">A handle to an open registry key, or a predefined key constant.</param>
	/// <param name="lpSubKey">The path of the subkey of <paramref name="hkey"/>.</param>
	/// <param name="lpValue">The name of the registry value.</param>
	/// <param name="dwFlags">Flags specifying the type of data to retrieve.</param>
	/// <param name="pdwType">Receives the type of data stored in the registry value.</param>
	/// <param name="pvData">A pointer to a buffer that receives the value data, or <see cref="IntPtr.Zero"/> to query the required buffer size.</param>
	/// <param name="pcbData">On input, the size of the buffer pointed to by <paramref name="pvData"/>; on output, the size of the retrieved data.</param>
	/// <returns>A Win32 error code; zero indicates success.</returns>
	[DllImport("advapi32", CharSet = CharSet.Unicode, SetLastError = true)]
	public static extern int RegGetValue(UIntPtr hkey, string lpSubKey, string lpValue, RegistryFlags dwFlags, out RegistryType pdwType, IntPtr pvData, ref uint pcbData);
}
