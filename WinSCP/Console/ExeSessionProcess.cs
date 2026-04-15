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

	/// <summary>Maximum number of attempts to find a unique console instance name.</summary>
	private const int MaxAttempts = 10;

	/// <summary>Base name of the shared-memory file-mapping object used for console communication.</summary>
	private const string ConsoleMapping = "WinSCPConsoleMapping";

	/// <summary>Base name of the event object signalled by the WinSCP process when a console request is ready.</summary>
	private const string ConsoleEventRequest = "WinSCPConsoleEventRequest";

	/// <summary>Base name of the event object signalled by the .NET wrapper when a console response has been written.</summary>
	private const string ConsoleEventResponse = "WinSCPConsoleEventResponse";

	/// <summary>Base name of the event object used to cancel a pending console request.</summary>
	private const string ConsoleEventCancel = "WinSCPConsoleEventCancel";

	/// <summary>Base name of the Windows Job object used to guard the WinSCP child process.</summary>
	private const string ConsoleJob = "WinSCPConsoleJob";

	/// <summary>File name of the WinSCP executable.</summary>
	private const string ExeExecutableFileName = "winscp.exe";

	/// <summary>The managed <see cref="Process"/> object for the WinSCP child process.</summary>
	private Process _process;

	/// <summary>Lock object that serialises access to process-lifecycle fields.</summary>
	private readonly object _lock = new object();

	/// <summary>The diagnostic logger used to record internal events.</summary>
	private readonly Logger _logger;

	/// <summary>The WinSCP session that owns this process.</summary>
	private readonly Session _session;

	/// <summary>Event signalled by the WinSCP process to notify the .NET wrapper of a pending console request.</summary>
	private EventWaitHandle _requestEvent;

	/// <summary>Event signalled by the .NET wrapper after it has processed a console request.</summary>
	private EventWaitHandle _responseEvent;

	/// <summary>Event that can be signalled to cancel a pending console operation.</summary>
	private EventWaitHandle _cancelEvent;

	/// <summary>Safe handle to the shared-memory file mapping used for console communication.</summary>
	private SafeFileHandle _fileMapping;

	/// <summary>The unique suffix appended to all named kernel objects for this process instance.</summary>
	private string _instanceName;

	/// <summary>Background thread that pumps console events from the WinSCP process.</summary>
	private Thread _thread;

	/// <summary>When <see langword="true"/>, signals the event-pump thread to stop processing.</summary>
	private bool _abort;

	/// <summary>
	/// The most recent "from-beginning" message that is buffered pending a newline
	/// so that progress-style overwrite lines are handled correctly.
	/// </summary>
	private string _lastFromBeginning;

	/// <summary>Accumulates characters that have been received but do not yet form a complete line.</summary>
	private string _incompleteLine;

	/// <summary>Queue of command strings waiting to be fed to the WinSCP process via stdin.</summary>
	private readonly List<string> _input = new List<string>();

	/// <summary>Queue of log messages corresponding to each entry in <see cref="_input"/>.</summary>
	private readonly List<string> _log = new List<string>();

	/// <summary>Event set whenever a new entry is added to <see cref="_input"/> so the event-pump thread wakes up.</summary>
	private AutoResetEvent _inputEvent = new AutoResetEvent(initialState: false);

	/// <summary>Windows Job object that keeps the WinSCP child process alive only as long as the current process.</summary>
	private Job _job;

	/// <summary>When <see langword="true"/>, the next progress event will signal cancellation to the WinSCP process.</summary>
	private bool _cancel;

	/// <summary>
	/// Cache mapping (executable path, last-write timestamp) pairs to their
	/// <see cref="FileVersionInfo"/> to avoid repeated version checks for the same binary.
	/// </summary>
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

	/// <summary>
	/// Initializes a new instance of <see cref="ExeSessionProcess"/>, resolves the WinSCP
	/// executable path, validates its version, and prepares the <see cref="Process"/>
	/// start information.
	/// </summary>
	/// <param name="session">The WinSCP session that owns this process.</param>
	/// <param name="useXmlLog">
	/// When <see langword="true"/>, passes <c>/xmllog</c> arguments so that WinSCP writes
	/// a structured XML log that the .NET wrapper reads back. Set to <see langword="false"/>
	/// for console-only sessions.
	/// </param>
	/// <param name="additionalArguments">
	/// Any extra command-line arguments to append, or <see langword="null"/> for none.
	/// </param>
	/// <exception cref="SessionLocalException">
	/// Thrown when the WinSCP executable cannot be found or its version does not match
	/// this assembly.
	/// </exception>
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

	/// <summary>
	/// Escapes a file path for use as a WinSCP command-line argument by quoting it
	/// and doubling any exclamation marks to prevent them being interpreted as history
	/// expansions.
	/// </summary>
	/// <param name="path">The file-system path to escape.</param>
	/// <returns>The escaped path string suitable for embedding in a command-line argument.</returns>
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

	/// <summary>
	/// Appends the console instance arguments to the process start information, optionally
	/// grants the configured user access to the window station and desktop, starts the
	/// WinSCP child process, and launches the background event-pump thread.
	/// </summary>
	/// <exception cref="SessionLocalException">
	/// Thrown when granting access to the window station or desktop fails.
	/// </exception>
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

	/// <summary>
	/// Grants the configured executable process user the specified access rights on a
	/// window-station or desktop kernel object.
	/// </summary>
	/// <param name="handle">A raw handle to the window-station or desktop object.</param>
	/// <param name="accessMask">The access rights to grant, as a Win32 access-mask integer.</param>
	private void GrantAccess(IntPtr handle, int accessMask)
	{
		using SafeHandle safeHandle = new NoopSafeHandle(handle);
		GenericSecurity genericSecurity = new GenericSecurity(isContainer: false, ResourceType.WindowObject, safeHandle, AccessControlSections.Access);
		genericSecurity.AddAccessRule(new GenericAccessRule(new NTAccount(_session.ExecutableProcessUserName), accessMask, AccessControlType.Allow));
		genericSecurity.Persist(safeHandle, AccessControlSections.Access);
	}

	/// <summary>
	/// Handles the <see cref="Process.Exited"/> event by logging the process exit code.
	/// </summary>
	/// <param name="sender">The source of the event.</param>
	/// <param name="e">An <see cref="EventArgs"/> that contains no event data.</param>
	private void ProcessExited(object sender, EventArgs e)
	{
		_logger.WriteLine("Process {0} exited with exit code {1}", _process.Id, _process.ExitCode);
	}

	/// <summary>
	/// Returns <see langword="true"/> if the event-pump should stop, either because
	/// <see cref="_abort"/> was set or because the WinSCP child process has exited.
	/// </summary>
	/// <returns>
	/// <see langword="true"/> if processing should stop; <see langword="false"/> otherwise.
	/// </returns>
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

	/// <summary>
	/// Entry point for the background event-pump thread. Waits for the request event and
	/// dispatches each console event until the process exits or <see cref="_abort"/> is set.
	/// </summary>
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

	/// <summary>
	/// Acquires the shared-memory communication structure, determines the current event type,
	/// dispatches it to the appropriate handler, and signals the response event.
	/// </summary>
	/// <exception cref="NotImplementedException">Thrown when an unknown event type is encountered.</exception>
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

	/// <summary>
	/// Handles a <see cref="ConsoleEvent.Choice"/> event by raising the session's query
	/// handler and writing the selected result back into the event structure.
	/// </summary>
	/// <param name="e">The choice-event structure to read options from and write the result to.</param>
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

	/// <summary>
	/// Handles a <see cref="ConsoleEvent.Title"/> event by logging the requested title.
	/// Title changes are not applied to the current console window.
	/// </summary>
	/// <param name="e">The title-event structure containing the requested window title.</param>
	private void ProcessTitleEvent(ConsoleTitleEventStruct e)
	{
		using (_logger.CreateCallstack())
		{
			_logger.WriteLine("Not-supported title event [{0}]", e.Title);
		}
	}

	/// <summary>
	/// Handles a <see cref="ConsoleEvent.Input"/> event by dequeuing the next pending input
	/// string and writing it into the event structure, blocking until one is available or
	/// the process exits.
	/// </summary>
	/// <param name="e">The input-event structure to populate with the next input line.</param>
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

	/// <summary>
	/// Processes a raw message from the WinSCP process and forwards complete lines to
	/// <see cref="OutputDataReceived"/> subscribers, buffering "from-beginning"
	/// (carriage-return style) messages until a newline is received.
	/// </summary>
	/// <param name="fromBeginning">
	/// When <see langword="true"/>, the message should overwrite the current output line
	/// rather than being appended.
	/// </param>
	/// <param name="error">
	/// When <see langword="true"/>, the message is error output.
	/// </param>
	/// <param name="message">The raw text received from the WinSCP process.</param>
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

	/// <summary>
	/// Splits <paramref name="message"/> into lines, raises <see cref="OutputDataReceived"/>
	/// for each complete line, and stores any trailing partial line in
	/// <see cref="_incompleteLine"/> for the next call.
	/// </summary>
	/// <param name="message">The text to append to the current output, may span multiple lines.</param>
	/// <param name="error">When <see langword="true"/>, the lines are tagged as error output.</param>
	private void AddToOutput(string message, bool error)
	{
		string[] array = (_incompleteLine + message).Split('\n');
		_incompleteLine = array[array.Length - 1];
		for (int i = 0; i < array.Length - 1; i++)
		{
			this.OutputDataReceived?.Invoke(this, new OutputDataReceivedEventArgs(array[i], error));
		}
	}

	/// <summary>
	/// Handles a <see cref="ConsoleEvent.Print"/> event by forwarding the message to
	/// <see cref="Print"/>.
	/// </summary>
	/// <param name="e">The print-event structure containing the message and output flags.</param>
	private void ProcessPrintEvent(ConsolePrintEventStruct e)
	{
		_logger.WriteLineLevel(1, string.Format(CultureInfo.CurrentCulture, "Print: {0}", new object[1] { e.Message }));
		Print(e.FromBeginning, e.Error, e.Message);
	}

	/// <summary>
	/// Handles a <see cref="ConsoleEvent.Init"/> event by verifying the expected console
	/// interface options and writing the negotiated I/O type and progress preference back
	/// into the event structure.
	/// </summary>
	/// <param name="e">The init-event structure to read from and write negotiated settings to.</param>
	/// <exception cref="InvalidOperationException">
	/// Thrown when the WinSCP process reports unexpected console interface options.
	/// </exception>
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

	/// <summary>
	/// Handles a <see cref="ConsoleEvent.Progress"/> event by translating the native
	/// progress data into a <see cref="FileTransferProgressEventArgs"/> and raising it
	/// through the session, then writing any cancellation request back into the structure.
	/// </summary>
	/// <param name="e">The progress-event structure to read from and write cancellation state to.</param>
	/// <exception cref="ArgumentOutOfRangeException">
	/// Thrown when the event contains an unknown operation or progress-side value.
	/// </exception>
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

	/// <summary>
	/// Handles a <see cref="ConsoleEvent.TransferOut"/> event by writing the received data
	/// chunk to <see cref="StdOut"/>, or closing the write end of the pipe when the chunk
	/// length is zero (end-of-stream signal).
	/// </summary>
	/// <param name="e">The transfer-event structure containing the data buffer and length.</param>
	/// <exception cref="InvalidOperationException">
	/// Thrown when data is received but <see cref="StdOut"/> has not been configured.
	/// </exception>
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

	/// <summary>
	/// Handles a <see cref="ConsoleEvent.TransferIn"/> event by reading up to
	/// <c>e.Len</c> bytes from <see cref="StdIn"/> and writing them back into the event
	/// structure, or marking the structure as failed on I/O error.
	/// </summary>
	/// <param name="e">
	/// The transfer-event structure specifying the requested read length and used to return
	/// the data and actual bytes read.
	/// </param>
	/// <exception cref="InvalidOperationException">
	/// Thrown when data is requested but <see cref="StdIn"/> has not been configured.
	/// </exception>
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

	/// <summary>
	/// Creates all named kernel objects required for console communication (request event,
	/// response event, cancel event, file mapping, and optionally a Job object), retrying
	/// with different random suffixes until a unique instance name is found.
	/// </summary>
	/// <exception cref="SessionLocalException">
	/// Thrown when a unique instance name cannot be found within <see cref="MaxAttempts"/>
	/// attempts, or when the file-mapping object cannot be created.
	/// </exception>
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

	/// <summary>
	/// Creates a named shared-memory file-mapping object of the size required by
	/// <see cref="ConsoleCommStruct.Size"/>, applying a DACL for the configured
	/// executable process user when one is set.
	/// </summary>
	/// <param name="fileMappingName">The system-wide name for the file-mapping object.</param>
	/// <returns>A <see cref="SafeFileHandle"/> wrapping the created file-mapping handle.</returns>
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

	/// <summary>
	/// Maps the shared-memory file into the current process and returns a new
	/// <see cref="ConsoleCommStruct"/> that provides typed access to its contents.
	/// </summary>
	/// <returns>A new <see cref="ConsoleCommStruct"/> backed by the shared-memory region.</returns>
	private ConsoleCommStruct AcquireCommStruct()
	{
		return new ConsoleCommStruct(_session, _fileMapping);
	}

	/// <summary>
	/// Attempts to create a named auto-reset <see cref="EventWaitHandle"/>, applying a
	/// DACL for the configured executable process user when one is set.
	/// </summary>
	/// <param name="name">The system-wide name for the event object.</param>
	/// <param name="ev">
	/// When this method returns, contains the newly created (or existing) event handle.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if the event was newly created;
	/// <see langword="false"/> if an event with that name already existed.
	/// </returns>
	private bool TryCreateEvent(string name, out EventWaitHandle ev)
	{
		_logger.WriteLine("Creating event {0}", name);
		EventWaitHandleSecurity eventWaitHandleSecurity = CreateSecurity(EventWaitHandleRights.FullControl);
		ev = new EventWaitHandle(initialState: false, EventResetMode.AutoReset, name, out var createdNew);//(initialState: false, EventResetMode.AutoReset, name, out var createdNew, eventWaitHandleSecurity);
		string text = ((eventWaitHandleSecurity != null) ? eventWaitHandleSecurity.GetSecurityDescriptorSddlForm(AccessControlSections.All) : "none");
		_logger.WriteLine("Created event {0} with handle {1} with security {2}, new {3}", name, ev.SafeWaitHandle.DangerousGetHandle(), text, createdNew);
		return createdNew;
	}

	/// <summary>
	/// Builds an <see cref="EventWaitHandleSecurity"/> DACL that grants the configured
	/// executable process user the specified rights, or returns <see langword="null"/>
	/// when no process user name is configured.
	/// </summary>
	/// <param name="eventRights">The access rights to grant to the configured user.</param>
	/// <returns>
	/// A populated <see cref="EventWaitHandleSecurity"/> instance, or
	/// <see langword="null"/> when no user name is configured.
	/// </returns>
	/// <exception cref="SessionLocalException">
	/// Thrown when the configured user account name cannot be resolved to an
	/// <see cref="System.Security.Principal.IdentityReference"/>.
	/// </exception>
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

	/// <summary>
	/// Creates a new named auto-reset event, throwing if the name is already in use.
	/// </summary>
	/// <param name="name">The system-wide name for the event object.</param>
	/// <returns>The newly created <see cref="EventWaitHandle"/>.</returns>
	/// <exception cref="SessionLocalException">
	/// Thrown when an event with the given name already exists.
	/// </exception>
	private EventWaitHandle CreateEvent(string name)
	{
		if (!TryCreateEvent(name, out var ev))
		{
			throw _logger.WriteException(new SessionLocalException(_session, string.Format(CultureInfo.InvariantCulture, "Event {0} already exists", new object[1] { name })));
		}
		return ev;
	}

	/// <summary>
	/// When handle-close testing is enabled on the session, verifies that the named event
	/// object has been closed by trying to create it exclusively and logging a warning if
	/// it still exists.
	/// </summary>
	/// <param name="name">The system-wide name of the event object to test.</param>
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

	/// <summary>
	/// Validates that <paramref name="str"/> fits within the
	/// <see cref="ConsoleInputEventStruct.Str"/> field size limit, then enqueues the
	/// string and its log counterpart and signals the input-available event.
	/// </summary>
	/// <param name="str">The command string to send to the WinSCP process.</param>
	/// <param name="log">The log-safe representation of the command (may differ from <paramref name="str"/> for sensitive data).</param>
	/// <exception cref="InvalidOperationException">
	/// Thrown when the <see cref="MarshalAsAttribute"/> on <c>ConsoleInputEventStruct.Str</c>
	/// cannot be found.
	/// </exception>
	/// <exception cref="SessionLocalException">
	/// Thrown when <paramref name="str"/> exceeds the maximum allowed input length.
	/// </exception>
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

    /// <summary>
    /// Resolves the full path to the WinSCP executable, either from the explicit
    /// <see cref="Session.ExecutablePath"/> setting or by searching well-known locations.
    /// </summary>
    /// <returns>The absolute path to <c>winscp.exe</c>.</returns>
    /// <exception cref="SessionLocalException">
    /// Thrown when an explicit path is configured but the file does not exist, or when
    /// automatic search fails to locate the executable.
    /// </exception>
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

    /// <summary>
    /// Searches a prioritised list of candidate directories for <c>winscp.exe</c> and
    /// returns the first path where it is found.
    /// </summary>
    /// <param name="session">The session whose logger and configuration are used during the search.</param>
    /// <returns>The absolute path to the located <c>winscp.exe</c> file.</returns>
    /// <exception cref="SessionLocalException">
    /// Thrown when the executable cannot be found in any of the inspected locations.
    /// </exception>
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

    /// <summary>
    /// Returns the default WinSCP installation directory based on the current process
    /// bitness (<c>%ProgramFiles(x86)%\WinSCP</c> for 64-bit, <c>%ProgramFiles%\WinSCP</c>
    /// for 32-bit).
    /// </summary>
    /// <returns>The expected default installation directory path.</returns>
    private static string GetDefaultInstallationPath()
    {
        string path = ((IntPtr.Size != 8) ? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) : Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86));
        return Path.Combine(path, "WinSCP");
    }

    /// <summary>
    /// Reads the WinSCP installation path from the Windows registry under the specified hive.
    /// </summary>
    /// <param name="hive">The registry hive (<see cref="RegistryHive.CurrentUser"/> or
    /// <see cref="RegistryHive.LocalMachine"/>) to search.</param>
    /// <returns>
    /// The installation directory recorded in the Inno Setup uninstall key, or
    /// <see langword="null"/> when the key is absent.
    /// </returns>
    private static string GetInstallationPath(RegistryHive hive)
    {
        RegistryKey registryKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry32).OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\winscp3_is1");
        if (registryKey == null)
        {
            return null;
        }
        return (string)registryKey.GetValue("Inno Setup: App Path");
    }

    /// <summary>
    /// Checks whether <c>winscp.exe</c> exists in the given directory, adding the directory
    /// to <paramref name="paths"/> to avoid searching it again.
    /// </summary>
    /// <param name="logger">The logger used to record search progress.</param>
    /// <param name="paths">The list of directories already searched; updated by this call.</param>
    /// <param name="path">The directory to search, or <see langword="null"/> to skip.</param>
    /// <param name="result">
    /// When this method returns <see langword="true"/>, the full path to <c>winscp.exe</c>;
    /// otherwise <see langword="null"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if <c>winscp.exe</c> was found in <paramref name="path"/>;
    /// otherwise <see langword="false"/>.
    /// </returns>
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

    /// <summary>
    /// Returns the directory containing the WinSCP .NET wrapper assembly, as determined
    /// by the logger's assembly file path.
    /// </summary>
    /// <param name="logger">The logger whose assembly path is queried.</param>
    /// <returns>
    /// The directory of the wrapper assembly, or <see langword="null"/> when the path
    /// cannot be determined.
    /// </returns>
    private static string GetAssemblyPath(Logger logger)
    {
        return DoGetAssemblyPath(logger.GetAssemblyFilePath());
    }

    /// <summary>
    /// Returns the directory containing the application entry-point assembly, as determined
    /// by the logger's entry-assembly file path.
    /// </summary>
    /// <param name="logger">The logger whose entry-assembly path is queried.</param>
    /// <returns>
    /// The directory of the entry-point assembly, or <see langword="null"/> when the path
    /// cannot be determined.
    /// </returns>
    private static string GetEntryAssemblyPath(Logger logger)
    {
        return DoGetAssemblyPath(logger.GetEntryAssemblyFilePath());
    }

    /// <summary>
    /// Extracts the directory component from a code-base file path.
    /// </summary>
    /// <param name="codeBasePath">
    /// The full path to an assembly file, or <see langword="null"/> / empty.
    /// </param>
    /// <returns>
    /// The directory containing the assembly, or <see langword="null"/> when
    /// <paramref name="codeBasePath"/> is null or empty.
    /// </returns>
    private static string DoGetAssemblyPath(string codeBasePath)
    {
        string result = null;
        if (!string.IsNullOrEmpty(codeBasePath))
        {
            result = Path.GetDirectoryName(codeBasePath);
        }
        return result;
    }

    /// <summary>
    /// Retrieves the size, in bytes, of the version-information resource for the specified file.
    /// </summary>
    /// <param name="lptstrFilename">The path to the file whose version-info size is queried.</param>
    /// <param name="handle">Receives a handle used by <c>GetFileVersionInfo</c>; can be ignored.</param>
    /// <returns>The size of the version-information resource, or zero on failure.</returns>
    [DllImport("version.dll", BestFitMapping = false, CharSet = CharSet.Auto, SetLastError = true)]
    public static extern int GetFileVersionInfoSize(string lptstrFilename, out int handle);

    /// <summary>
    /// Loads the specified module into the address space of the calling process with extended options.
    /// </summary>
    /// <param name="lpFileName">The name of the module to load.</param>
    /// <param name="hReservedNull">Reserved; must be <see cref="IntPtr.Zero"/>.</param>
    /// <param name="dwFlags">Action to take when loading the module.</param>
    /// <returns>A handle to the loaded module, or <see cref="IntPtr.Zero"/> on failure.</returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hReservedNull, uint dwFlags);

    /// <summary>
    /// Decrements the reference count of a loaded module and unloads it when the count reaches zero.
    /// </summary>
    /// <param name="hModule">A handle to the loaded module.</param>
    /// <returns><see langword="true"/> if the function succeeds; otherwise <see langword="false"/>.</returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FreeLibrary(IntPtr hModule);

    /// <summary>
    /// Determines the location of a resource with the specified type and name in the specified module.
    /// </summary>
    /// <param name="hModule">A handle to the module whose portable executable image is to be searched.</param>
    /// <param name="lpName">The name of the resource.</param>
    /// <param name="lpType">The resource type.</param>
    /// <returns>A handle to the specified resource's information block, or <see cref="IntPtr.Zero"/> on failure.</returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr FindResource(IntPtr hModule, string lpName, string lpType);

    /// <summary>
    /// Returns the size, in bytes, of the specified resource.
    /// </summary>
    /// <param name="hModule">A handle to the module whose portable executable image contains the resource.</param>
    /// <param name="hResInfo">A handle to the resource information block whose size is to be returned.</param>
    /// <returns>The number of bytes in the resource, or zero on failure.</returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint SizeofResource(IntPtr hModule, IntPtr hResInfo);

    /// <summary>
    /// Formats a <see cref="FileVersionInfo"/> into a human-readable string that includes
    /// the file version and product name and version, for use in log and exception messages.
    /// </summary>
    /// <param name="version">The version info to format.</param>
    /// <returns>A formatted version string.</returns>
    private string GetVersionStr(FileVersionInfo version)
    {
        return version.FileVersion + ", product " + version.ProductName + " version is " + version.ProductVersion;
    }

    /// <summary>
    /// Verifies that the product version of the WinSCP executable at
    /// <paramref name="exePath"/> matches the product version of the .NET wrapper
    /// assembly, caching the result to avoid redundant checks.
    /// </summary>
    /// <param name="exePath">The full path to the WinSCP executable to check.</param>
    /// <param name="assemblyVersion">
    /// The version information for the .NET wrapper assembly, or <see langword="null"/>
    /// when the assembly version is not available.
    /// </param>
    /// <exception cref="SessionLocalException">
    /// Thrown when the executable version does not match the assembly version and version
    /// checking has not been disabled via <see cref="Session.DisableVersionCheck"/>.
    /// </exception>
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
