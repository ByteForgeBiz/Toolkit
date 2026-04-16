using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace ByteForge.WinSCP;

/// <summary>
/// Provides a managed view of the shared-memory region used for console-event
/// communication between the WinSCP executable and the .NET wrapper.
/// Maps the memory-mapped file into the current process, reads the
/// <see cref="ConsoleCommHeader"/>, and marshals the event-specific payload
/// on demand. Implements <see cref="IDisposable"/>; unmapping flushes any
/// modified header or payload back to shared memory.
/// </summary>
internal class ConsoleCommStruct : IDisposable
{
	/// <summary>
	/// The console communication protocol version implemented by this assembly.
	/// </summary>
	public const int CurrentVersion = 10;

	/// <summary>
	/// Pointer to the start of the mapped memory region.
	/// </summary>
	private IntPtr _ptr;

	/// <summary>
	/// The header read from (and optionally written back to) shared memory.
	/// </summary>
	private readonly ConsoleCommHeader _header;

	/// <summary>
	/// Indicates whether <see cref="_header"/> has been modified and must be
	/// written back to shared memory on disposal.
	/// </summary>
	private bool _headerInvalidated;

	/// <summary>
	/// Pointer to the payload area immediately following the header in shared memory.
	/// </summary>
	private readonly IntPtr _payloadPtr;

	/// <summary>
	/// The lazily-unmarshalled payload object for the current event.
	/// </summary>
	private object _payload;

	/// <summary>
	/// The safe handle for the underlying memory-mapped file.
	/// </summary>
	private readonly SafeFileHandle _fileMapping;

	/// <summary>
	/// The WinSCP session that owns this communication structure.
	/// </summary>
	private readonly Session _session;

	/// <summary>
	/// Gets the type of console event currently stored in the communication structure.
	/// </summary>
	/// <value>The <see cref="ConsoleEvent"/> value from the mapped header.</value>
	public ConsoleEvent Event => _header.Event;

	/// <summary>
	/// Gets the payload interpreted as a <see cref="ConsolePrintEventStruct"/>.
	/// </summary>
	/// <value>The unmarshalled print-event payload.</value>
	/// <exception cref="InvalidOperationException">
	/// Thrown when the current event is not <see cref="ConsoleEvent.Print"/>.
	/// </exception>
	public ConsolePrintEventStruct PrintEvent => UnmarshalPayload<ConsolePrintEventStruct>(ConsoleEvent.Print);

	/// <summary>
	/// Gets the payload interpreted as a <see cref="ConsoleInitEventStruct"/>.
	/// </summary>
	/// <value>The unmarshalled initialization-event payload.</value>
	/// <exception cref="InvalidOperationException">
	/// Thrown when the current event is not <see cref="ConsoleEvent.Init"/>.
	/// </exception>
	public ConsoleInitEventStruct InitEvent => UnmarshalPayload<ConsoleInitEventStruct>(ConsoleEvent.Init);

	/// <summary>
	/// Gets the payload interpreted as a <see cref="ConsoleInputEventStruct"/>.
	/// </summary>
	/// <value>The unmarshalled input-event payload.</value>
	/// <exception cref="InvalidOperationException">
	/// Thrown when the current event is not <see cref="ConsoleEvent.Input"/>.
	/// </exception>
	public ConsoleInputEventStruct InputEvent => UnmarshalPayload<ConsoleInputEventStruct>(ConsoleEvent.Input);

	/// <summary>
	/// Gets the payload interpreted as a <see cref="ConsoleChoiceEventStruct"/>.
	/// </summary>
	/// <value>The unmarshalled choice-event payload.</value>
	/// <exception cref="InvalidOperationException">
	/// Thrown when the current event is not <see cref="ConsoleEvent.Choice"/>.
	/// </exception>
	public ConsoleChoiceEventStruct ChoiceEvent => UnmarshalPayload<ConsoleChoiceEventStruct>(ConsoleEvent.Choice);

	/// <summary>
	/// Gets the payload interpreted as a <see cref="ConsoleTitleEventStruct"/>.
	/// </summary>
	/// <value>The unmarshalled title-event payload.</value>
	/// <exception cref="InvalidOperationException">
	/// Thrown when the current event is not <see cref="ConsoleEvent.Title"/>.
	/// </exception>
	public ConsoleTitleEventStruct TitleEvent => UnmarshalPayload<ConsoleTitleEventStruct>(ConsoleEvent.Title);

	/// <summary>
	/// Gets the payload interpreted as a <see cref="ConsoleProgressEventStruct"/>.
	/// </summary>
	/// <value>The unmarshalled progress-event payload.</value>
	/// <exception cref="InvalidOperationException">
	/// Thrown when the current event is not <see cref="ConsoleEvent.Progress"/>.
	/// </exception>
	public ConsoleProgressEventStruct ProgressEvent => UnmarshalPayload<ConsoleProgressEventStruct>(ConsoleEvent.Progress);

	/// <summary>
	/// Gets the payload interpreted as a <see cref="ConsoleTransferEventStruct"/> for an
	/// outbound transfer (data produced by the WinSCP executable).
	/// </summary>
	/// <value>The unmarshalled transfer-out-event payload.</value>
	/// <exception cref="InvalidOperationException">
	/// Thrown when the current event is not <see cref="ConsoleEvent.TransferOut"/>.
	/// </exception>
	public ConsoleTransferEventStruct TransferOutEvent => UnmarshalPayload<ConsoleTransferEventStruct>(ConsoleEvent.TransferOut);

	/// <summary>
	/// Gets the payload interpreted as a <see cref="ConsoleTransferEventStruct"/> for an
	/// inbound transfer (data to be consumed by the WinSCP executable).
	/// </summary>
	/// <value>The unmarshalled transfer-in-event payload.</value>
	/// <exception cref="InvalidOperationException">
	/// Thrown when the current event is not <see cref="ConsoleEvent.TransferIn"/>.
	/// </exception>
	public ConsoleTransferEventStruct TransferInEvent => UnmarshalPayload<ConsoleTransferEventStruct>(ConsoleEvent.TransferIn);

	/// <summary>
	/// Gets the minimum number of bytes required for the shared-memory region: the size of
	/// <see cref="ConsoleCommHeader"/> plus the size of the largest event-payload structure.
	/// </summary>
	/// <value>Total size in bytes of the communication structure.</value>
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

	/// <summary>
	/// Initializes a new instance of <see cref="ConsoleCommStruct"/> by mapping the
	/// memory-mapped file into the current process and reading the header.
	/// </summary>
	/// <param name="session">The session that owns this communication structure.</param>
	/// <param name="fileMapping">A safe handle to the memory-mapped file to map into the process.</param>
	/// <exception cref="SessionLocalException">
	/// Thrown if the memory-mapped file cannot be mapped into the process address space.
	/// </exception>
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

	/// <summary>
	/// Finalizes the instance, ensuring the mapped view is unmapped if
	/// <see cref="Dispose"/> was not called explicitly.
	/// </summary>
	~ConsoleCommStruct()
	{
		Dispose();
	}

	/// <summary>
	/// Unmaps the shared-memory view, flushing any modified header or payload back to
	/// shared memory, and suppresses the finalizer.
	/// </summary>
	/// <exception cref="SessionLocalException">
	/// Thrown if the call to <c>UnmapViewOfFile</c> fails.
	/// </exception>
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

	/// <summary>
	/// Unmarshals the payload region of shared memory as the specified type, caching the
	/// result so subsequent calls for the same event return the same object instance.
	/// </summary>
	/// <typeparam name="T">The managed type to unmarshal the payload into.</typeparam>
	/// <param name="e">The expected <see cref="ConsoleEvent"/> that corresponds to <typeparamref name="T"/>.</param>
	/// <returns>The unmarshalled payload object cast to <typeparamref name="T"/>.</returns>
	/// <exception cref="InvalidOperationException">
	/// Thrown when <paramref name="e"/> does not match the current <see cref="Event"/>,
	/// or when the instance has already been disposed.
	/// </exception>
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

	/// <summary>
	/// Verifies that this instance has not been disposed.
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown when the instance has already been disposed.</exception>
	private void CheckNotDisposed()
	{
		if (_ptr == IntPtr.Zero)
		{
			throw _session.Logger.WriteException(new InvalidOperationException("Object is disposed"));
		}
	}

	/// <summary>
	/// Initializes the communication header with the current protocol version and resets
	/// the event type to <see cref="ConsoleEvent.None"/>, marking the header as dirty so
	/// it will be written back to shared memory on <see cref="Dispose"/>.
	/// </summary>
	public void InitHeader()
	{
		_headerInvalidated = true;
		_header.Size = (uint)Size;
		_header.Version = 10;
		_header.Event = ConsoleEvent.None;
	}
}
