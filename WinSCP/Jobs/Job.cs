using System;
using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

internal class Job : IDisposable
{
	private IntPtr _handle;

	private readonly Logger _logger;

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

	~Job()
	{
		DoDispose();
	}

	public void Dispose()
	{
		DoDispose();
		GC.SuppressFinalize(this);
	}

	private void DoDispose()
	{
		Close();
	}

	public void Close()
	{
		_logger.WriteLine("Closing job");
		UnsafeNativeMethods.CloseHandle(_handle);
		_handle = IntPtr.Zero;
	}
}
