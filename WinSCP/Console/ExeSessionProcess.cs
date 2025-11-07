using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Threading;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace ByteForge.WinSCP;

/// <summary>
/// Represents a session process for executing WinSCP commands and handling console events.
/// </summary>
internal class ExeSessionProcess : IDisposable
{
	/// <summary>
	/// Represents a safe handle that does not require releasing.
	/// </summary>
	internal class NoopSafeHandle : SafeHandle
	{
		/// <summary>
		/// Gets a value indicating whether the handle is invalid.
		/// </summary>
		public override bool IsInvalid => false;

		/// <summary>
		/// Initializes a new instance of the <see cref="NoopSafeHandle"/> class.
		/// </summary>
		/// <param name="handle">The handle to wrap.</param>
		public NoopSafeHandle(IntPtr handle)
			: base(handle, ownsHandle: false)
		{
		}

		/// <summary>
		/// Releases the handle. Always returns true.
		/// </summary>
		/// <returns>True.</returns>
		protected override bool ReleaseHandle()
		{
			return true;
		}
	}

	private const int MaxAttempts = 10;

	private const string ConsoleMapping = "WinSCPConsoleMapping";

	private const string ConsoleEventRequest = "WinSCPConsoleEventRequest";

	private const string ConsoleEventResponse = "WinSCPConsoleEventResponse";

	private const string ConsoleEventCancel = "WinSCPConsoleEventCancel";

	private const string ConsoleJob = "WinSCPConsoleJob";

	private const string ExeExecutableFileName = "winscp.exe";

	private Process _process;

	private readonly object _lock = new object();

	private readonly Logger _logger;

	private readonly Session _session;

	private EventWaitHandle _requestEvent;

	private EventWaitHandle _responseEvent;

	private EventWaitHandle _cancelEvent;

	private SafeFileHandle _fileMapping;

	private string _instanceName;

	private Thread _thread;

	private bool _abort;

	private string _lastFromBeginning;

	private string _incompleteLine;

	private readonly List<string> _input = new List<string>();

	private readonly List<string> _log = new List<string>();

	private AutoResetEvent _inputEvent = new AutoResetEvent(initialState: false);

	private Job _job;

	private bool _cancel;

	private static readonly Dictionary<Tuple<string, DateTime>, FileVersionInfo> _versionInfoCache = new Dictionary<Tuple<string, DateTime>, FileVersionInfo>();

	/// <summary>
	/// Gets a value indicating whether the process has exited.
	/// </summary>
	public bool HasExited => _process.HasExited;

	/// <summary>
	/// Gets the exit code of the process.
	/// </summary>
	public int ExitCode => _process.ExitCode;

	/// <summary>
	/// Gets or sets the standard output stream.
	/// </summary>
	public PipeStream StdOut { get; set; }

	/// <summary>
	/// Gets or sets the standard input stream.
	/// </summary>
	public Stream StdIn { get; set; }

	/// <summary>
	/// Gets the path to the executable.
	/// </summary>
	public string ExecutablePath { get; }

	/// <summary>
	/// Occurs when output data is received.
	/// </summary>
	public event OutputDataReceivedEventHandler OutputDataReceived;

	/// <summary>
	/// Creates a new <see cref="ExeSessionProcess"/> for a session.
	/// </summary>
	/// <param name="session">The session to associate with the process.</param>
	/// <returns>A new <see cref="ExeSessionProcess"/> instance.</returns>
	public static ExeSessionProcess CreateForSession(Session session)
	{
		return new ExeSessionProcess(session, useXmlLog: true, null);
	}

	/// <summary>
	/// Creates a new <see cref="ExeSessionProcess"/> for a console session with additional arguments.
	/// </summary>
	/// <param name="session">The session to associate with the process.</param>
	/// <param name="additionalArguments">Additional arguments for the process.</param>
	/// <returns>A new <see cref="ExeSessionProcess"/> instance.</returns>
	public static ExeSessionProcess CreateForConsole(Session session, string additionalArguments)
	{
		return new ExeSessionProcess(session, useXmlLog: false, additionalArguments);
	}

	private ExeSessionProcess(Session session, bool useXmlLog, string additionalArguments)
	{
		_session = session;
		_logger = session.Logger;
		_incompleteLine = string.Empty;
		using (_logger.CreateCallstack())
		{
            ExecutablePath = GetExecutablePath();
			_logger.WriteLine("EXE executable path resolved to {0}", ExecutablePath);
			string assemblyFilePath = _logger.GetAssemblyFilePath();
			FileVersionInfo fileVersionInfo = null;
			if (assemblyFilePath != null)
			{
				fileVersionInfo = FileVersionInfo.GetVersionInfo(assemblyFilePath);
			}
			CheckVersion(ExecutablePath, fileVersionInfo);
			string text = (_session.DefaultConfigurationInternal ? "/ini=nul " : (string.IsNullOrEmpty(_session.IniFilePathInternal) ? "" : string.Format(CultureInfo.InvariantCulture, "/ini=\"{0}\" ", new object[1] { _session.IniFilePathInternal })));
			string text2 = null;
			if (!string.IsNullOrEmpty(_session.SessionLogPath))
			{
				text2 = string.Format(CultureInfo.InvariantCulture, "/log=\"{0}\" ", new object[1] { LogPathEscape(_session.SessionLogPath) });
			}
			string text3 = ((!useXmlLog) ? "" : string.Format(CultureInfo.InvariantCulture, "/xmllog=\"{0}\" /xmlgroups /xmllogrequired ", new object[1] { LogPathEscape(_session.XmlLogPath) }));
			string text4 = null;
			if (_session.DebugLogLevel != 0)
			{
				text4 = string.Format(CultureInfo.InvariantCulture, "/loglevel={0} ", new object[1] { _session.DebugLogLevel });
			}
			string text5 = ((fileVersionInfo == null) ? "unk" : string.Format(CultureInfo.InvariantCulture, "{0}.{1}.{2} ", new object[3] { fileVersionInfo.ProductMajorPart, fileVersionInfo.ProductMinorPart, fileVersionInfo.ProductBuildPart }));
			string text6 = string.Format(CultureInfo.InvariantCulture, "/dotnet={0} ", new object[1] { text5 });
			string arguments = text3 + "/nointeractiveinput /stdout /stdin " + text6 + text + text2 + text4 + _session.AdditionalExecutableArguments;
			Tools.AddRawParameters(ref arguments, _session.RawConfiguration, "/rawconfig", count: false);
			if (!string.IsNullOrEmpty(additionalArguments))
			{
				arguments = arguments + " " + additionalArguments;
			}
			_process = new Process();
			_process.StartInfo.FileName = ExecutablePath;
			_process.StartInfo.WorkingDirectory = Path.GetDirectoryName(ExecutablePath);
			_process.StartInfo.Arguments = arguments;
			_process.StartInfo.UseShellExecute = false;
			_process.Exited += ProcessExited;
		}
	}

	private static string LogPathEscape(string path)
	{
		return Tools.ArgumentEscape(path).Replace("!", "!!");
	}

	/// <summary>
	/// Aborts the process by killing it if it is running.
	/// </summary>
	public void Abort()
	{
		using (_logger.CreateCallstack())
		{
			lock (_lock)
			{
				if (_process != null && !_process.HasExited)
				{
					_process.Kill();
				}
			}
		}
	}

	/// <summary>
	/// Starts the process and initializes the console and child process.
	/// </summary>
	public void Start()
	{
		using (_logger.CreateCallstack())
		{
			InitializeConsole();
			InitializeChild();
		}
	}

	private void InitializeChild()
	{
		using (_logger.CreateCallstack())
		{
			_process.StartInfo.Arguments += string.Format(CultureInfo.InvariantCulture, " /console /consoleinstance={0}", new object[1] { _instanceName });
			if (!string.IsNullOrEmpty(_session.ExecutableProcessUserName))
			{
				_logger.WriteLine("Will run process as {0}", _session.ExecutableProcessUserName);
				_process.StartInfo.UserName = _session.ExecutableProcessUserName;
				_process.StartInfo.Password = _session.ExecutableProcessPassword;
				_process.StartInfo.LoadUserProfile = true;
				_logger.WriteLine("Granting access to window station");
				try
				{
					IntPtr processWindowStation = UnsafeNativeMethods.GetProcessWindowStation();
					GrantAccess(processWindowStation, 983935);
				}
				catch (Exception innerException)
				{
					throw _logger.WriteException(new SessionLocalException(_session, "Error granting access to window station", innerException));
				}
				_logger.WriteLine("Granting access to desktop");
				try
				{
					IntPtr threadDesktop = UnsafeNativeMethods.GetThreadDesktop(UnsafeNativeMethods.GetCurrentThreadId());
					GrantAccess(threadDesktop, 983551);
				}
				catch (Exception innerException2)
				{
					throw _logger.WriteException(new SessionLocalException(_session, "Error granting access to desktop", innerException2));
				}
			}
			_logger.WriteLine("Starting \"{0}\" {1}", _process.StartInfo.FileName, _process.StartInfo.Arguments);
			_process.Start();
			_logger.WriteLine("Started process {0}", _process.Id);
			_thread = new Thread(ProcessEvents)
			{
				IsBackground = true
			};
			_thread.Start();
		}
	}

	private void GrantAccess(IntPtr handle, int accessMask)
	{
		using SafeHandle safeHandle = new NoopSafeHandle(handle);
		GenericSecurity genericSecurity = new GenericSecurity(isContainer: false, ResourceType.WindowObject, safeHandle, AccessControlSections.Access);
		genericSecurity.AddAccessRule(new GenericAccessRule(new NTAccount(_session.ExecutableProcessUserName), accessMask, AccessControlType.Allow));
		genericSecurity.Persist(safeHandle, AccessControlSections.Access);
	}

	private void ProcessExited(object sender, EventArgs e)
	{
		_logger.WriteLine("Process {0} exited with exit code {1}", _process.Id, _process.ExitCode);
	}

	private bool AbortedOrExited()
	{
		if (_abort)
		{
			_logger.WriteLine("Aborted");
			return true;
		}
		if (_process.HasExited)
		{
			_logger.WriteLine("Exited");
			return true;
		}
		return false;
	}

	private void ProcessEvents()
	{
		using (_logger.CreateCallstack())
		{
			try
			{
				while (!AbortedOrExited())
				{
					_logger.WriteLineLevel(1, "Waiting for request event");
					if (_requestEvent.WaitOne(100, exitContext: false))
					{
						_logger.WriteLineLevel(1, "Got request event");
						ProcessEvent();
					}
					if (_logger.LogLevel >= 1)
					{
						_logger.WriteLine(string.Format(CultureInfo.InvariantCulture, "2nd generation collection count: {0}", new object[1] { GC.CollectionCount(2) }));
						_logger.WriteLine(string.Format(CultureInfo.InvariantCulture, "Total memory allocated: {0}", new object[1] { GC.GetTotalMemory(forceFullCollection: false) }));
					}
				}
			}
			catch (Exception e)
			{
				_logger.WriteLine("Error while processing events");
				_logger.WriteException(e);
				throw;
			}
		}
	}

	private void ProcessEvent()
	{
		using (_logger.CreateCallstack())
		{
			using (ConsoleCommStruct consoleCommStruct = AcquireCommStruct())
			{
				switch (consoleCommStruct.Event)
				{
				case ConsoleEvent.Print:
					ProcessPrintEvent(consoleCommStruct.PrintEvent);
					break;
				case ConsoleEvent.Input:
					ProcessInputEvent(consoleCommStruct.InputEvent);
					break;
				case ConsoleEvent.Choice:
					ProcessChoiceEvent(consoleCommStruct.ChoiceEvent);
					break;
				case ConsoleEvent.Title:
					ProcessTitleEvent(consoleCommStruct.TitleEvent);
					break;
				case ConsoleEvent.Init:
					ProcessInitEvent(consoleCommStruct.InitEvent);
					break;
				case ConsoleEvent.Progress:
					ProcessProgressEvent(consoleCommStruct.ProgressEvent);
					break;
				case ConsoleEvent.TransferOut:
					ProcessTransferOutEvent(consoleCommStruct.TransferOutEvent);
					break;
				case ConsoleEvent.TransferIn:
					ProcessTransferInEvent(consoleCommStruct.TransferInEvent);
					break;
				default:
					throw _logger.WriteException(new NotImplementedException());
				}
			}
			_responseEvent.Set();
			_logger.WriteLineLevel(1, "Response event set");
		}
	}

	private void ProcessChoiceEvent(ConsoleChoiceEventStruct e)
	{
		using (_logger.CreateCallstack())
		{
			_logger.WriteLine("Options: [{0}], Timer: [{1}], Timeouting: [{2}], Timeouted: [{3}], Break: [{4}]", e.Options, e.Timer, e.Timeouting, e.Timeouted, e.Break);
			QueryReceivedEventArgs e2 = new QueryReceivedEventArgs
			{
				Message = e.Message
			};
			_session.ProcessChoice(e2);
			if (e2.SelectedAction == QueryReceivedEventArgs.Action.None)
			{
				if (e.Timeouting)
				{
					Thread.Sleep((int)e.Timer);
					e.Result = e.Timeouted;
				}
				else
				{
					e.Result = e.Break;
				}
			}
			else if (e2.SelectedAction == QueryReceivedEventArgs.Action.Continue)
			{
				if (e.Timeouting)
				{
					Thread.Sleep((int)e.Timer);
					e.Result = e.Timeouted;
				}
				else
				{
					e.Result = e.Continue;
				}
			}
			else if (e2.SelectedAction == QueryReceivedEventArgs.Action.Abort)
			{
				e.Result = e.Break;
			}
			_logger.WriteLine("Options Result: [{0}]", e.Result);
		}
	}

	private void ProcessTitleEvent(ConsoleTitleEventStruct e)
	{
		using (_logger.CreateCallstack())
		{
			_logger.WriteLine("Not-supported title event [{0}]", e.Title);
		}
	}

	private void ProcessInputEvent(ConsoleInputEventStruct e)
	{
		using (_logger.CreateCallstack())
		{
			while (!AbortedOrExited())
			{
				lock (_input)
				{
					if (_input.Count > 0)
					{
						e.Str = _input[0];
						e.Result = true;
						_input.RemoveAt(0);
						Print(fromBeginning: false, error: false, _log[0] + "\n");
						_log.RemoveAt(0);
						break;
					}
				}
				_inputEvent.WaitOne(100, exitContext: false);
			}
		}
	}

	private void Print(bool fromBeginning, bool error, string message)
	{
		if (fromBeginning && (message.Length == 0 || message[0] != '\n'))
		{
			_lastFromBeginning = message;
			_logger.WriteLine("Buffered from-beginning message [{0}]", _lastFromBeginning);
			this.OutputDataReceived?.Invoke(this, null);
			return;
		}
		if (!string.IsNullOrEmpty(_lastFromBeginning))
		{
			AddToOutput(_lastFromBeginning, error: false);
			_lastFromBeginning = null;
		}
		if (fromBeginning && message.Length > 0 && message[0] == '\n')
		{
			AddToOutput("\n", error: false);
			_lastFromBeginning = message.Substring(1);
			_logger.WriteLine("Buffered from-beginning message [{0}]", _lastFromBeginning);
		}
		else
		{
			AddToOutput(message, error);
		}
	}

	private void AddToOutput(string message, bool error)
	{
		string[] array = (_incompleteLine + message).Split('\n');
		_incompleteLine = array[array.Length - 1];
		for (int i = 0; i < array.Length - 1; i++)
		{
			this.OutputDataReceived?.Invoke(this, new OutputDataReceivedEventArgs(array[i], error));
		}
	}

	private void ProcessPrintEvent(ConsolePrintEventStruct e)
	{
		_logger.WriteLineLevel(1, string.Format(CultureInfo.CurrentCulture, "Print: {0}", new object[1] { e.Message }));
		Print(e.FromBeginning, e.Error, e.Message);
	}

	private void ProcessInitEvent(ConsoleInitEventStruct e)
	{
		using (_logger.CreateCallstack())
		{
			if (!e.UseStdErr || e.BinaryOutput != ConsoleInitEventStruct.StdInOut.Binary || e.BinaryInput != ConsoleInitEventStruct.StdInOut.Binary)
			{
				throw _logger.WriteException(new InvalidOperationException("Unexpected console interface options"));
			}
			e.InputType = 3u;
			e.OutputType = 3u;
			e.WantsProgress = _session.WantsProgress;
		}
	}

	private void ProcessProgressEvent(ConsoleProgressEventStruct e)
	{
		using (_logger.CreateCallstack())
		{
			_logger.WriteLine("File Name [{0}] - Directory [{1}] - Overall Progress [{2}] - File Progress [{3}] - CPS [{4}]", e.FileName, e.Directory, e.OverallProgress, e.FileProgress, e.CPS);
			if (!_cancel)
			{
				FileTransferProgressEventArgs e2 = new FileTransferProgressEventArgs();
				if (e.Operation != ConsoleProgressEventStruct.ProgressOperation.Copy)
				{
					throw _logger.WriteException(new ArgumentOutOfRangeException("Unknown progress operation", (Exception)null));
				}
				e2.Operation = ProgressOperation.Transfer;
				switch (e.Side)
				{
				case ConsoleProgressEventStruct.ProgressSide.Local:
					e2.Side = ProgressSide.Local;
					break;
				case ConsoleProgressEventStruct.ProgressSide.Remote:
					e2.Side = ProgressSide.Remote;
					break;
				default:
					throw _logger.WriteException(new ArgumentOutOfRangeException("Unknown progress side", (Exception)null));
				}
				e2.FileName = e.FileName;
				e2.Directory = e.Directory;
				e2.OverallProgress = (double)e.OverallProgress / 100.0;
				e2.FileProgress = (double)e.FileProgress / 100.0;
				e2.CPS = (int)e.CPS;
				e2.Cancel = false;
				_session.ProcessProgress(e2);
			}
			if (_cancel)
			{
				e.Cancel = true;
			}
		}
	}

	private void ProcessTransferOutEvent(ConsoleTransferEventStruct e)
	{
		using (_logger.CreateCallstack())
		{
			_logger.WriteLine("Len [{0}]", e.Len);
			if (StdOut == null)
			{
				throw _logger.WriteException(new InvalidOperationException("Unexpected data"));
			}
			int len = (int)e.Len;
			if (len > 0)
			{
				StdOut.WriteInternal(e.Data, 0, len);
				_logger.WriteLine("Data written to the buffer");
			}
			else
			{
				StdOut.CloseWrite();
				_logger.WriteLine("Data buffer closed");
			}
		}
	}

	private void ProcessTransferInEvent(ConsoleTransferEventStruct e)
	{
		using (_logger.CreateCallstack())
		{
			_logger.WriteLine("Len [{0}]", e.Len);
			if (StdIn == null)
			{
				throw _logger.WriteException(new InvalidOperationException("Unexpected data request"));
			}
			try
			{
				int len = (int)e.Len;
				len = StdIn.Read(e.Data, 0, len);
				_logger.WriteLine("{0} bytes read", len);
				e.Len = (uint)len;
			}
			catch (Exception e2)
			{
				_logger.WriteLine("Error reading data stream");
				_logger.WriteException(e2);
				e.Error = true;
			}
		}
	}

	private void InitializeConsole()
	{
		using (_logger.CreateCallstack())
		{
			int num = 0;
			Random random = new Random();
			int id = Process.GetCurrentProcess().Id;
			do
			{
				if (num > 10)
				{
					throw _logger.WriteException(new SessionLocalException(_session, "Cannot find unique name for event object."));
				}
				int num2 = random.Next(1000);
				_instanceName = string.Format(CultureInfo.InvariantCulture, "_{0}_{1}_{2}", new object[3]
				{
					id,
					GetHashCode(),
					num2
				});
				_logger.WriteLine("Trying event {0}", _instanceName);
				if (!TryCreateEvent("WinSCPConsoleEventRequest" + _instanceName, out _requestEvent))
				{
					_logger.WriteLine("Event {0} is not unique", _instanceName);
					_requestEvent.Close();
					_requestEvent = null;
				}
				else
				{
					_logger.WriteLine("Event {0} is unique", _instanceName);
					_responseEvent = CreateEvent("WinSCPConsoleEventResponse" + _instanceName);
					_cancelEvent = CreateEvent("WinSCPConsoleEventCancel" + _instanceName);
					string text = "WinSCPConsoleMapping" + _instanceName;
					_fileMapping = CreateFileMapping(text);
					if (Marshal.GetLastWin32Error() == 183)
					{
						throw _logger.WriteException(new SessionLocalException(_session, string.Format(CultureInfo.InvariantCulture, "File mapping {0} already exists", new object[1] { text })));
					}
					if (_fileMapping.IsInvalid)
					{
						throw _logger.WriteException(new SessionLocalException(_session, string.Format(CultureInfo.InvariantCulture, "Cannot create file mapping {0}", new object[1] { text })));
					}
				}
				num++;
			}
			while (_requestEvent == null);
			using (ConsoleCommStruct consoleCommStruct = AcquireCommStruct())
			{
				consoleCommStruct.InitHeader();
			}
			if (_session.GuardProcessWithJobInternal)
			{
				string name = "WinSCPConsoleJob" + _instanceName;
				_job = new Job(_logger, name);
			}
		}
	}

	private unsafe SafeFileHandle CreateFileMapping(string fileMappingName)
	{
		IntPtr intPtr = IntPtr.Zero;
		EventWaitHandleSecurity eventWaitHandleSecurity = CreateSecurity((EventWaitHandleRights)983071);
		if (eventWaitHandleSecurity != null)
		{
			SecurityAttributes securityAttributes = default(SecurityAttributes);
			securityAttributes.nLength = (uint)Marshal.SizeOf((object)securityAttributes);
			byte[] securityDescriptorBinaryForm = eventWaitHandleSecurity.GetSecurityDescriptorBinaryForm();
			byte* ptr = stackalloc byte[(int)(uint)securityDescriptorBinaryForm.Length];
			for (int i = 0; i < securityDescriptorBinaryForm.Length; i++)
			{
				ptr[i] = securityDescriptorBinaryForm[i];
			}
			securityAttributes.lpSecurityDescriptor = (IntPtr)ptr;
			intPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(SecurityAttributes)));
			Marshal.StructureToPtr((object)securityAttributes, intPtr, fDeleteOld: false);
		}
		return UnsafeNativeMethods.CreateFileMapping(new SafeFileHandle(new IntPtr(-1), ownsHandle: true), intPtr, FileMapProtection.PageReadWrite, 0, ConsoleCommStruct.Size, fileMappingName);
	}

	private ConsoleCommStruct AcquireCommStruct()
	{
		return new ConsoleCommStruct(_session, _fileMapping);
	}

	private bool TryCreateEvent(string name, out EventWaitHandle ev)
	{
		_logger.WriteLine("Creating event {0}", name);
		EventWaitHandleSecurity eventWaitHandleSecurity = CreateSecurity(EventWaitHandleRights.FullControl);
		ev = new EventWaitHandle(initialState: false, EventResetMode.AutoReset, name, out var createdNew);//(initialState: false, EventResetMode.AutoReset, name, out var createdNew, eventWaitHandleSecurity);
		string text = ((eventWaitHandleSecurity != null) ? eventWaitHandleSecurity.GetSecurityDescriptorSddlForm(AccessControlSections.All) : "none");
		_logger.WriteLine("Created event {0} with handle {1} with security {2}, new {3}", name, ev.SafeWaitHandle.DangerousGetHandle(), text, createdNew);
		return createdNew;
	}

	private EventWaitHandleSecurity CreateSecurity(EventWaitHandleRights eventRights)
	{
		EventWaitHandleSecurity eventWaitHandleSecurity = null;
		if (!string.IsNullOrEmpty(_session.ExecutableProcessUserName))
		{
			eventWaitHandleSecurity = new EventWaitHandleSecurity();
			IdentityReference identity;
			try
			{
				identity = new NTAccount(_session.ExecutableProcessUserName);
			}
			catch (Exception innerException)
			{
				throw _logger.WriteException(new SessionLocalException(_session, string.Format(CultureInfo.CurrentCulture, "Error resolving account {0}", new object[1] { _session.ExecutableProcessUserName }), innerException));
			}
			EventWaitHandleAccessRule rule = new EventWaitHandleAccessRule(identity, eventRights, AccessControlType.Allow);
			eventWaitHandleSecurity.AddAccessRule(rule);
		}
		return eventWaitHandleSecurity;
	}

	private EventWaitHandle CreateEvent(string name)
	{
		if (!TryCreateEvent(name, out var ev))
		{
			throw _logger.WriteException(new SessionLocalException(_session, string.Format(CultureInfo.InvariantCulture, "Event {0} already exists", new object[1] { name })));
		}
		return ev;
	}

	private void TestEventClosed(string name)
	{
		if (_session.TestHandlesClosedInternal)
		{
			_logger.WriteLine("Testing that event {0} is closed", name);
			if (TryCreateEvent(name, out var ev))
			{
				ev.Close();
				return;
			}
			_logger.WriteLine("Exception: Event {0} was not closed yet", name);
		}
	}

	private void AddInput(string str, string log)
	{
		object[] customAttributes = typeof(ConsoleInputEventStruct).GetField("Str").GetCustomAttributes(typeof(MarshalAsAttribute), inherit: false);
		if (customAttributes.Length != 1)
		{
			throw _logger.WriteException(new InvalidOperationException("MarshalAs attribute not found for ConsoleInputEventStruct.Str"));
		}
		MarshalAsAttribute marshalAsAttribute = (MarshalAsAttribute)customAttributes[0];
		if (marshalAsAttribute.SizeConst <= str.Length)
		{
			throw _logger.WriteException(new SessionLocalException(_session, string.Format(CultureInfo.CurrentCulture, "Input [{0}] is too long ({1} limit)", new object[2] { str, marshalAsAttribute.SizeConst })));
		}
		lock (_input)
		{
			_input.Add(str);
			_log.Add(log);
			_inputEvent.Set();
		}
	}

	/// <summary>
	/// Executes a command in the session process.
	/// </summary>
	/// <param name="command">The command to execute.</param>
	/// <param name="log">The log message associated with the command.</param>
	public void ExecuteCommand(string command, string log)
	{
		using (_logger.CreateCallstack())
		{
			_cancel = false;
			AddInput(command, log);
		}
	}

	/// <summary>
	/// Closes the process, waiting for it to exit or killing it if necessary.
	/// </summary>
	public void Close()
	{
		using (_logger.CreateCallstack())
		{
			int num = 2000;
			_logger.WriteLine("Waiting for process to exit ({0} ms)", num);
			if (!_process.WaitForExit(num))
			{
				_logger.WriteLine("Killing process");
				_process.Kill();
			}
		}
	}

    private string GetExecutablePath()
    {
        using (_logger.CreateCallstack())
        {
            string executablePath = _session.ExecutablePath;
            if (!string.IsNullOrEmpty(executablePath))
            {
                if (!File.Exists(executablePath))
                {
                    throw _logger.WriteException(new SessionLocalException(_session, executablePath + " does not exists."));
                }
                return executablePath;
            }
            return FindExecutable(_session);
        }
    }

    internal static string FindExecutable(Session session)
    {
        Logger logger = session.Logger;
        List<string> list = new List<string>();
        string assemblyPath = GetAssemblyPath(logger);
        string path = ((!string.IsNullOrEmpty(assemblyPath)) ? null : Path.GetDirectoryName(Logger.GetProcessPath()));
        if (!TryFindExecutableInPath(logger, list, assemblyPath, out var result) && !TryFindExecutableInPath(logger, list, GetEntryAssemblyPath(logger), out result) && !TryFindExecutableInPath(logger, list, path, out result) && !TryFindExecutableInPath(logger, list, GetInstallationPath(RegistryHive.CurrentUser), out result) && !TryFindExecutableInPath(logger, list, GetInstallationPath(RegistryHive.LocalMachine), out result) && !TryFindExecutableInPath(logger, list, GetDefaultInstallationPath(), out result))
        {
            string text = string.Join(", ", list);
            string text2 = "winscp.exe";
            string message = "The " + text2 + " executable was not found at any of the inspected locations (" + text + "). You may use Session.ExecutablePath property to explicitly set path to " + text2 + ".";
            throw logger.WriteException(new SessionLocalException(session, message));
        }
        return result;
    }

    private static string GetDefaultInstallationPath()
    {
        string path = ((IntPtr.Size != 8) ? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) : Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86));
        return Path.Combine(path, "WinSCP");
    }

    private static string GetInstallationPath(RegistryHive hive)
    {
        RegistryKey registryKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry32).OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\winscp3_is1");
        if (registryKey == null)
        {
            return null;
        }
        return (string)registryKey.GetValue("Inno Setup: App Path");
    }

    private static bool TryFindExecutableInPath(Logger logger, List<string> paths, string path, out string result)
    {
        if (string.IsNullOrEmpty(path))
        {
            result = null;
        }
        else if (paths.Contains(path, StringComparer.CurrentCultureIgnoreCase))
        {
            logger.WriteLine("Already searched " + path);
            result = null;
        }
        else
        {
            paths.Add(path);
            string text = Path.Combine(path, "winscp.exe");
            if (File.Exists(text))
            {
                result = text;
                logger.WriteLine("Executable found in {0}", text);
            }
            else
            {
                result = null;
                logger.WriteLine("Executable not found in {0}", text);
            }
        }
        return result != null;
    }

    private static string GetAssemblyPath(Logger logger)
    {
        return DoGetAssemblyPath(logger.GetAssemblyFilePath());
    }

    private static string GetEntryAssemblyPath(Logger logger)
    {
        return DoGetAssemblyPath(logger.GetEntryAssemblyFilePath());
    }

    private static string DoGetAssemblyPath(string codeBasePath)
    {
        string result = null;
        if (!string.IsNullOrEmpty(codeBasePath))
        {
            result = Path.GetDirectoryName(codeBasePath);
        }
        return result;
    }

    [DllImport("version.dll", BestFitMapping = false, CharSet = CharSet.Auto, SetLastError = true)]
    public static extern int GetFileVersionInfoSize(string lptstrFilename, out int handle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hReservedNull, uint dwFlags);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FreeLibrary(IntPtr hModule);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr FindResource(IntPtr hModule, string lpName, string lpType);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint SizeofResource(IntPtr hModule, IntPtr hResInfo);

    private string GetVersionStr(FileVersionInfo version)
    {
        return version.FileVersion + ", product " + version.ProductName + " version is " + version.ProductVersion;
    }

    private void CheckVersion(string exePath, FileVersionInfo assemblyVersion)
    {
        using (_logger.CreateCallstack())
        {
            if (assemblyVersion == null)
            {
                _logger.WriteLine("Assembly version not known, cannot check version");
                return;
            }
            if (assemblyVersion.ProductVersion == "9.9.9.9")
            {
                _logger.WriteLine("Undefined assembly version, cannot check version");
                return;
            }
            DateTime lastWriteTimeUtc = File.GetLastWriteTimeUtc(exePath);
            _logger.WriteLine($"Timestamp of {exePath} is {lastWriteTimeUtc}");
            Tuple<string, DateTime> key = new Tuple<string, DateTime>(exePath, lastWriteTimeUtc);
            bool flag;
            lock (_versionInfoCache)
            {
                flag = _versionInfoCache.TryGetValue(key, out var value);
                if (flag)
                {
                    _logger.WriteLine("Cached version of " + exePath + " is " + GetVersionStr(value) + ", and it was already deemed compatible");
                }
                else
                {
                    _logger.WriteLine($"Executable version is not cached yet, cache size is {_versionInfoCache.Count}");
                }
            }
            if (flag)
            {
                return;
            }
            FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(exePath);
            _logger.WriteLine("Version of " + exePath + " is " + GetVersionStr(versionInfo));
            bool flag2 = assemblyVersion.ProductVersion != versionInfo.ProductVersion;
            Exception ex = null;
            if (flag2 || _logger.Logging)
            {
                try
                {
                    using (File.OpenRead(exePath))
                    {
                    }
                    long length = new FileInfo(exePath).Length;
                    _logger.WriteLine($"Size of the executable file is {length}");
                    int handle;
                    int fileVersionInfoSize = GetFileVersionInfoSize(exePath, out handle);
                    if (fileVersionInfoSize == 0)
                    {
                        throw new Exception("Cannot retrieve " + exePath + " version info", new Win32Exception());
                    }
                    _logger.WriteLine($"Size of the executable file version info is {fileVersionInfoSize}");
                }
                catch (Exception ex2)
                {
                    _logger.WriteLine("Accessing executable file failed");
                    _logger.WriteException(ex2);
                    ex = ex2;
                }
            }
            if (_session.DisableVersionCheck)
            {
                _logger.WriteLine("Version check disabled (not recommended)");
                return;
            }
            if (flag2)
            {
                if (_logger.Logging)
                {
                    try
                    {
                        using SHA256 sHA = SHA256.Create();
                        using FileStream inputStream = File.OpenRead(exePath);
                        string text = string.Concat(Array.ConvertAll(sHA.ComputeHash(inputStream), (byte b) => b.ToString("x2")));
                        _logger.WriteLine("SHA-256 of the executable file is " + text);
                    }
                    catch (Exception e)
                    {
                        _logger.WriteLine("Calculating SHA-256 of the executable file failed");
                        _logger.WriteException(e);
                    }
                    try
                    {
                        IntPtr intPtr = LoadLibraryEx(exePath, IntPtr.Zero, 2u);
                        if (intPtr == IntPtr.Zero)
                        {
                            _logger.WriteLine("Cannot load");
                            _logger.WriteException(new Win32Exception());
                        }
                        else
                        {
                            IntPtr intPtr2 = FindResource(intPtr, "#1", "#16");
                            if (intPtr2 == IntPtr.Zero)
                            {
                                _logger.WriteLine("Cannot find version resource");
                                _logger.WriteException(new Win32Exception());
                            }
                            else
                            {
                                uint num = SizeofResource(intPtr, intPtr2);
                                if (num == 0)
                                {
                                    _logger.WriteLine("Cannot find size of version resource");
                                    _logger.WriteException(new Win32Exception());
                                }
                                else
                                {
                                    _logger.WriteLine($"Version resource size is {num}");
                                }
                            }
                            FreeLibrary(intPtr);
                        }
                    }
                    catch (Exception e2)
                    {
                        _logger.WriteLine("Querying version resource failed");
                        _logger.WriteException(e2);
                    }
                }
                string message = ((!string.IsNullOrEmpty(versionInfo.ProductVersion) || ex == null) ? ("The version of " + exePath + " (" + versionInfo.ProductVersion + ") does not match version of this assembly " + _logger.GetAssemblyFilePath() + " (" + assemblyVersion.ProductVersion + ").") : ("Cannot use " + exePath));
                throw _logger.WriteException(new SessionLocalException(_session, message, ex));
            }
            lock (_versionInfoCache)
            {
                _logger.WriteLine("Caching executable version");
                _versionInfoCache[key] = versionInfo;
            }
        }
    }

    /// <summary>
    /// Releases all resources used by the <see cref="ExeSessionProcess"/>.
    /// </summary>
    public void Dispose()
	{
		using (_logger.CreateCallstack())
		{
			lock (_lock)
			{
				if (_session.TestHandlesClosedInternal)
				{
					_logger.WriteLine("Will test that handles are closed");
				}
				_abort = true;
				if (_thread != null)
				{
					_thread.Join();
					_thread = null;
				}
				if (_process != null)
				{
					_process.Dispose();
					_process = null;
				}
				if (_requestEvent != null)
				{
					_requestEvent.Close();
					TestEventClosed("WinSCPConsoleEventRequest" + _instanceName);
				}
				if (_responseEvent != null)
				{
					_responseEvent.Close();
					TestEventClosed("WinSCPConsoleEventResponse" + _instanceName);
				}
				if (_cancelEvent != null)
				{
					_cancelEvent.Close();
					TestEventClosed("WinSCPConsoleEventCancel" + _instanceName);
				}
				if (_fileMapping != null)
				{
					_fileMapping.Dispose();
					_fileMapping = null;
					if (_session.TestHandlesClosedInternal)
					{
						_logger.WriteLine("Testing that file mapping is closed");
						string text = "WinSCPConsoleMapping" + _instanceName;
						SafeFileHandle safeFileHandle = CreateFileMapping(text);
						if (Marshal.GetLastWin32Error() == 183)
						{
							_logger.WriteLine("Exception: File mapping {0} was not closed yet", text);
						}
						if (!safeFileHandle.IsInvalid)
						{
							safeFileHandle.Dispose();
						}
					}
				}
				if (_inputEvent != null)
				{
					_inputEvent.Close();
					_inputEvent = null;
				}
				if (_job != null)
				{
					_job.Dispose();
					_job = null;
				}
			}
		}
	}

	/// <summary>
	/// Writes the status of the executable path to the logger.
	/// </summary>
	public void WriteStatus()
	{
		_logger.WriteLine("{0} - exists [{1}]", ExecutablePath, File.Exists(ExecutablePath));
	}

	/// <summary>
	/// Requests the callstack from the process.
	/// </summary>
	public void RequestCallstack()
	{
		using (_logger.CreateCallstack())
		{
			lock (_lock)
			{
				if (_process == null)
				{
					_logger.WriteLine("Process is closed already");
					return;
				}
				try
				{
					string text = string.Format(CultureInfo.InvariantCulture, "WinSCPCallstack{0}", new object[1] { _process.Id });
					using EventWaitHandle eventWaitHandle = EventWaitHandle.OpenExisting(text);
					_logger.WriteLine("Setting event {0}", text);
					eventWaitHandle.Set();
					string path = string.Format(CultureInfo.InvariantCulture, "{0}.txt", new object[1] { text });
					string text2 = Path.Combine(Path.GetTempPath(), path);
					int num = 2000;
					while (!File.Exists(text2))
					{
						if (num < 0)
						{
							throw new TimeoutException(string.Format(CultureInfo.CurrentCulture, "Timeout waiting for callstack file {0} to be created ", new object[1] { text2 }));
						}
						num -= 50;
						Thread.Sleep(50);
					}
					_logger.WriteLine("Callstack file {0} has been created", text2);
					Thread.Sleep(100);
					_logger.WriteLine(File.ReadAllText(text2));
					File.Delete(text2);
				}
				catch (Exception e)
				{
					_logger.WriteException(e);
				}
			}
		}
	}

	/// <summary>
	/// Cancels the current operation.
	/// </summary>
	public void Cancel()
	{
		_cancel = true;
	}
}
