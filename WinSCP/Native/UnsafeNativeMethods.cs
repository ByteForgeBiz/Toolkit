using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace ByteForge.WinSCP;

internal static class UnsafeNativeMethods
{
	public const int ERROR_ALREADY_EXISTS = 183;

	[DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
	public static extern SafeFileHandle CreateFileMapping(SafeFileHandle hFile, IntPtr lpAttributes, FileMapProtection fProtect, int dwMaximumSizeHigh, int dwMaximumSizeLow, string lpName);

	[DllImport("kernel32", ExactSpelling = true, SetLastError = true)]
	public static extern IntPtr MapViewOfFile(SafeFileHandle handle, FileMapAccess dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, UIntPtr dwNumberOfBytesToMap);

	[DllImport("kernel32", ExactSpelling = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

	[DllImport("kernel32", CharSet = CharSet.Auto, ExactSpelling = true, SetLastError = true)]
	public static extern int CloseHandle(IntPtr hObject);

	[DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
	public static extern IntPtr CreateJobObject(IntPtr a, string lpName);

	[DllImport("kernel32", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool SetInformationJobObject(IntPtr hJob, JobObjectInfoType infoType, IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern IntPtr GetProcessWindowStation();

	[DllImport("user32.dll", SetLastError = true)]
	public static extern IntPtr GetThreadDesktop(int dwThreadId);

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern int GetCurrentThreadId();

	[DllImport("advapi32", CharSet = CharSet.Unicode, SetLastError = true)]
	public static extern int RegGetValue(UIntPtr hkey, string lpSubKey, string lpValue, RegistryFlags dwFlags, out RegistryType pdwType, IntPtr pvData, ref uint pcbData);
}
