using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace ByteForge.WinSCP;

internal class ConsoleCommStruct : IDisposable
{
	public const int CurrentVersion = 10;

	private IntPtr _ptr;

	private readonly ConsoleCommHeader _header;

	private bool _headerInvalidated;

	private readonly IntPtr _payloadPtr;

	private object _payload;

	private readonly SafeFileHandle _fileMapping;

	private readonly Session _session;

	public ConsoleEvent Event => _header.Event;

	public ConsolePrintEventStruct PrintEvent => UnmarshalPayload<ConsolePrintEventStruct>(ConsoleEvent.Print);

	public ConsoleInitEventStruct InitEvent => UnmarshalPayload<ConsoleInitEventStruct>(ConsoleEvent.Init);

	public ConsoleInputEventStruct InputEvent => UnmarshalPayload<ConsoleInputEventStruct>(ConsoleEvent.Input);

	public ConsoleChoiceEventStruct ChoiceEvent => UnmarshalPayload<ConsoleChoiceEventStruct>(ConsoleEvent.Choice);

	public ConsoleTitleEventStruct TitleEvent => UnmarshalPayload<ConsoleTitleEventStruct>(ConsoleEvent.Title);

	public ConsoleProgressEventStruct ProgressEvent => UnmarshalPayload<ConsoleProgressEventStruct>(ConsoleEvent.Progress);

	public ConsoleTransferEventStruct TransferOutEvent => UnmarshalPayload<ConsoleTransferEventStruct>(ConsoleEvent.TransferOut);

	public ConsoleTransferEventStruct TransferInEvent => UnmarshalPayload<ConsoleTransferEventStruct>(ConsoleEvent.TransferIn);

	public static int Size
	{
		get
		{
			Type[] obj = new Type[7]
			{
				typeof(ConsolePrintEventStruct),
				typeof(ConsoleInitEventStruct),
				typeof(ConsoleInputEventStruct),
				typeof(ConsoleChoiceEventStruct),
				typeof(ConsoleTitleEventStruct),
				typeof(ConsoleProgressEventStruct),
				typeof(ConsoleTransferEventStruct)
			};
			int num = 0;
			Type[] array = obj;
			foreach (Type t in array)
			{
				num = Math.Max(num, Marshal.SizeOf(t));
			}
			return Marshal.SizeOf(typeof(ConsoleCommHeader)) + num;
		}
	}

	public ConsoleCommStruct(Session session, SafeFileHandle fileMapping)
	{
		_session = session;
		_fileMapping = fileMapping;
		_session.Logger.WriteLineLevel(1, "Acquiring communication structure");
		_ptr = UnsafeNativeMethods.MapViewOfFile(_fileMapping, FileMapAccess.FileMapAllAccess, 0u, 0u, UIntPtr.Zero);
		_session.Logger.WriteLineLevel(1, "Acquired communication structure");
		_payloadPtr = new IntPtr(_ptr.ToInt64() + 12);
		_header = (ConsoleCommHeader)Marshal.PtrToStructure(_ptr, typeof(ConsoleCommHeader));
	}

	~ConsoleCommStruct()
	{
		Dispose();
	}

	public void Dispose()
	{
		if (_ptr != IntPtr.Zero)
		{
			if (_headerInvalidated)
			{
				Marshal.StructureToPtr((object)_header, _ptr, fDeleteOld: false);
			}
			if (_payload != null && Event != ConsoleEvent.Print && Event != ConsoleEvent.Title)
			{
				Marshal.StructureToPtr(_payload, _payloadPtr, fDeleteOld: false);
			}
			_session.Logger.WriteLineLevel(1, "Releasing communication structure");
			if (!UnsafeNativeMethods.UnmapViewOfFile(_ptr))
			{
				throw _session.Logger.WriteException(new SessionLocalException(_session, "Cannot release file mapping"));
			}
			_session.Logger.WriteLineLevel(1, "Released communication structure");
			_ptr = IntPtr.Zero;
		}
		GC.SuppressFinalize(this);
	}

	private T UnmarshalPayload<T>(ConsoleEvent e)
	{
		CheckNotDisposed();
		if (e != Event)
		{
			throw _session.Logger.WriteException(new InvalidOperationException("Payload type does not match with event"));
		}
		if (_payload == null)
		{
			_payload = Marshal.PtrToStructure(_payloadPtr, typeof(T));
		}
		return (T)_payload;
	}

	private void CheckNotDisposed()
	{
		if (_ptr == IntPtr.Zero)
		{
			throw _session.Logger.WriteException(new InvalidOperationException("Object is disposed"));
		}
	}

	public void InitHeader()
	{
		_headerInvalidated = true;
		_header.Size = (uint)Size;
		_header.Version = 10;
		_header.Event = ConsoleEvent.None;
	}
}
