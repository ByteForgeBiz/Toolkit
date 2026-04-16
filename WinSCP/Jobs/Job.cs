using System;
using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

/// <summary>
/// Represents a Windows Job object that can be used to manage and limit child processes.
/// When the job is closed, all associated processes are terminated.
/// </summary>
internal class Job : IDisposable
{
	/// <summary>
	/// The native handle to the Windows Job object.
	/// </summary>
	private IntPtr _handle;

	/// <summary>
	/// The logger used to record job lifecycle events.
	/// </summary>
	private readonly Logger _logger;

	/// <summary>
	/// Initializes a new instance of the <see cref="Job"/> class, creating a named Windows
	/// Job object and configuring it to terminate all processes when the job is closed.
	/// </summary>
	/// <param name="logger">The logger used for recording diagnostic messages.</param>
	/// <param name="name">The name of the job object, used for identification.</param>
	public Job(Logger logger, string name)
	{
		_logger = logger;
		_handle = UnsafeNativeMethods.CreateJobObject(IntPtr.Zero, name);
		if (_handle == IntPtr.Zero)
		{
			_logger.WriteLine("Cannot create job ({0})", Logger.LastWin32ErrorMessage());
			return;
		}
		_logger.WriteLine("Job created");
		JobObjectBasicLimitInformation basicLimitInformation = new JobObjectBasicLimitInformation
		{
			LimitFlags = 8192u
		};
		JobObjectExtendedLimitInformation obj = new JobObjectExtendedLimitInformation
		{
			BasicLimitInformation = basicLimitInformation
		};
		int num = Marshal.SizeOf(typeof(JobObjectExtendedLimitInformation));
		IntPtr intPtr = Marshal.AllocHGlobal(num);
		Marshal.StructureToPtr((object)obj, intPtr, fDeleteOld: false);
		if (UnsafeNativeMethods.SetInformationJobObject(_handle, JobObjectInfoType.ExtendedLimitInformation, intPtr, (uint)num))
		{
			_logger.WriteLine("Job set to kill all processes");
			return;
		}
		_logger.WriteLine("Cannot set job to kill all processes ({0})", Logger.LastWin32ErrorMessage());
	}

	/// <summary>
	/// Finalizes the <see cref="Job"/> instance, releasing the native handle.
	/// </summary>
	~Job()
	{
		DoDispose();
	}

	/// <summary>
	/// Releases all resources used by the <see cref="Job"/> instance.
	/// </summary>
	public void Dispose()
	{
		DoDispose();
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Performs the actual resource cleanup by closing the job handle.
	/// </summary>
	private void DoDispose()
	{
		Close();
	}

	/// <summary>
	/// Closes the native job object handle, which causes all processes in the job to be terminated.
	/// </summary>
	public void Close()
	{
		_logger.WriteLine("Closing job");
		UnsafeNativeMethods.CloseHandle(_handle);
		_handle = IntPtr.Zero;
	}
}
