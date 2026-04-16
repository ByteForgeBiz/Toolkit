using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using Microsoft.Win32;

namespace ByteForge.WinSCP;

/// <summary>
/// Represents a WinSCP session for file transfer and remote operations.
/// </summary>
[Guid("56FFC5CE-3867-4EF0-A3B5-CFFBEB99EA35")]
[ClassInterface(ClassInterfaceType.AutoDispatch)]
[ComVisible(true)]
[ComSourceInterfaces(typeof(ISessionEvents))]
public sealed class Session : IDisposable, IReflect
{
   /// <summary>
	/// The XML namespace for WinSCP session schema.
	/// </summary>
	internal const string Namespace = "http://winscp.net/schema/session/1.0";

	/// <summary>
	/// The process used for the session.
	/// </summary>
	private ExeSessionProcess _process;

	/// <summary>
	/// The last output timestamp.
	/// </summary>
	private DateTime _lastOutput;

	/// <summary>
	/// The log reader for session elements.
	/// </summary>
	private ElementLogReader _reader;

	/// <summary>
	/// The session log reader.
	/// </summary>
	private SessionLogReader _logReader;

	/// <summary>
	/// The list of operation results.
	/// </summary>
	private readonly IList<OperationResultBase> _operationResults;

	/// <summary>
	/// The list of event actions.
	/// </summary>
	private readonly IList<Action> _events;

	/// <summary>
	/// The event used for signaling session events.
	/// </summary>
	private AutoResetEvent _eventsEvent;

	/// <summary>
	/// The event used for signaling user choices.
	/// </summary>
	private ManualResetEvent _choiceEvent;

	/// <summary>
	/// Indicates whether the session has been disposed.
	/// </summary>
	private bool _disposed;

	/// <summary>
	/// The path to the WinSCP executable.
	/// </summary>
	private string _executablePath;

	/// <summary>
	/// Additional arguments for the WinSCP executable.
	/// </summary>
	private string _additionalExecutableArguments;

	/// <summary>
	/// Indicates whether to use the default configuration.
	/// </summary>
	private bool _defaultConfiguration;

	/// <summary>
	/// Indicates whether to disable version check.
	/// </summary>
	private bool _disableVersionCheck;

	/// <summary>
	/// The path to the INI file.
	/// </summary>
	private string _iniFilePath;

	/// <summary>
	/// The reconnect time span.
	/// </summary>
	private TimeSpan _reconnectTime;

	/// <summary>
	/// The path to the session log file.
	/// </summary>
	private string _sessionLogPath;

  /// <summary>
	/// Indicates whether the session was aborted.
	/// </summary>
	private bool _aborted;

	/// <summary>
	/// Unique log identifier.
	/// </summary>
	private int _logUnique;

	/// <summary>
	/// The path to the XML log file.
	/// </summary>
	private string _xmlLogPath;

	/// <summary>
	/// The file transfer progress event handler.
	/// </summary>
	private FileTransferProgressEventHandler _fileTransferProgress;

	/// <summary>
	/// The progress handling mode.
	/// </summary>
	private int _progressHandling;

	/// <summary>
	/// Indicates whether to guard the process with a job object.
	/// </summary>
	private bool _guardProcessWithJob;

	/// <summary>
	/// The home path for the session.
	/// </summary>
	private string _homePath;

	/// <summary>
	/// The user name for the process running WinSCP.
	/// </summary>
	private string _executableProcessUserName;

	/// <summary>
	/// The password for the process running WinSCP.
	/// </summary>
	private SecureString _executableProcessPassword;

	/// <summary>
	/// The collection of error messages.
	/// </summary>
	private StringCollection _error;

	/// <summary>
	/// Indicates whether to ignore failed operations.
	/// </summary>
	private bool _ignoreFailed;

	/// <summary>
	/// The session timeout value.
	/// </summary>
	private TimeSpan _sessionTimeout;

	/// <summary>
	/// The query received event handler.
	/// </summary>
	private QueryReceivedEventHandler _queryReceived;

	/// <summary>
	/// Indicates whether to throw on standard output exception.
	/// </summary>
	private bool _throwStdOut;

	/// <summary>
	/// Gets or sets the path to the WinSCP executable.
	/// </summary>
	public string ExecutablePath
	{
		get
		{
			return GetExecutablePath();
		}
		set
		{
			CheckNotOpened();
			_executablePath = value;
		}
	}

	/// <summary>
	/// Gets or sets the user name for the process running WinSCP.
	/// </summary>
	public string ExecutableProcessUserName
	{
		get
		{
			return _executableProcessUserName;
		}
		set
		{
			CheckNotOpened();
			_executableProcessUserName = value;
		}
	}

	/// <summary>
	/// Gets or sets the password for the process running WinSCP.
	/// </summary>
	public SecureString ExecutableProcessPassword
	{
		get
		{
			return _executableProcessPassword;
		}
		set
		{
			CheckNotOpened();
			_executableProcessPassword = value;
		}
	}

	/// <summary>
	/// Gets or sets additional arguments for the WinSCP executable.
	/// </summary>
	public string AdditionalExecutableArguments
	{
		get
		{
			return _additionalExecutableArguments;
		}
		set
		{
			CheckNotOpened();
			_additionalExecutableArguments = value;
		}
	}

	/// <summary>
	/// Gets or sets a value indicating whether to use the default configuration.
	/// </summary>
	[Obsolete("Use AddRawConfiguration")]
	public bool DefaultConfiguration
	{
		get
		{
			return _defaultConfiguration;
		}
		set
		{
			CheckNotOpened();
			_defaultConfiguration = value;
		}
	}

	/// <summary>
	/// Gets or sets a value indicating whether to disable version check.
	/// </summary>
	public bool DisableVersionCheck
	{
		get
		{
			return _disableVersionCheck;
		}
		set
		{
			CheckNotOpened();
			_disableVersionCheck = value;
		}
	}

	/// <summary>
	/// Gets or sets the path to the INI file for configuration.
	/// </summary>
	[Obsolete("Use AddRawConfiguration")]
	public string IniFilePath
	{
		get
		{
			return _iniFilePath;
		}
		set
		{
			CheckNotOpened();
			_iniFilePath = value;
		}
	}

	/// <summary>
	/// Gets or sets the reconnect time interval.
	/// </summary>
	public TimeSpan ReconnectTime
	{
		get
		{
			return _reconnectTime;
		}
		set
		{
			CheckNotOpened();
			_reconnectTime = value;
		}
	}

	/// <summary>
	/// Gets or sets the reconnect time in milliseconds.
	/// </summary>
	public int ReconnectTimeInMilliseconds
	{
		get
		{
			return Tools.TimeSpanToMilliseconds(ReconnectTime);
		}
		set
		{
			ReconnectTime = Tools.MillisecondsToTimeSpan(value);
		}
	}

	/// <summary>
	/// Gets or sets the path to the debug log file.
	/// </summary>
	public string DebugLogPath
	{
		get
		{
			CheckNotDisposed();
			return Logger.LogPath;
		}
		set
		{
			CheckNotDisposed();
			Logger.LogPath = value;
		}
	}

	/// <summary>
	/// Gets or sets the debug log level.
	/// </summary>
	public int DebugLogLevel
	{
		get
		{
			CheckNotDisposed();
			return Logger.LogLevel;
		}
		set
		{
			CheckNotDisposed();
			Logger.LogLevel = value;
		}
	}

	/// <summary>
	/// Gets or sets the path to the session log file.
	/// </summary>
	public string SessionLogPath
	{
		get
		{
			return _sessionLogPath;
		}
		set
		{
			SetSessionLogPath(value);
		}
	}

	/// <summary>
	/// Gets or sets the path to the XML log file.
	/// </summary>
	public string XmlLogPath
	{
		get
		{
			return _xmlLogPath;
		}
		set
		{
			CheckNotOpened();
			_xmlLogPath = value;
		}
	}

	/// <summary>
	/// Gets or sets a value indicating whether to preserve the XML log file.
	/// </summary>
	public bool XmlLogPreserve { get; set; }

	/// <summary>
	/// Gets the home path of the session after opening.
	/// </summary>
	public string HomePath
	{
		get
		{
			CheckOpened();
			return _homePath;
		}
	}

	/// <summary>
	/// Gets or sets the session timeout interval.
	/// </summary>
	public TimeSpan Timeout { get; set; }

	/// <summary>
	/// Gets the output messages from the session.
	/// </summary>
	public StringCollection Output { get; private set; }

	/// <summary>
	/// Gets a value indicating whether the session is opened.
	/// </summary>
	public bool Opened
	{
		get
		{
			CheckNotDisposed();
			return _process != null;
		}
	}

	/// <summary>
	/// Gets a value indicating whether a file transfer progress handler is registered.
	/// </summary>
	/// <value><c>true</c> if a <see cref="FileTransferProgress"/> handler is subscribed; otherwise, <c>false</c>.</value>
	internal bool WantsProgress => _fileTransferProgress != null;

	/// <summary>
	/// Gets the underlying system type for COM reflection purposes.
	/// </summary>
	/// <value>The runtime <see cref="Type"/> of this instance.</value>
	Type IReflect.UnderlyingSystemType => GetType();

	/// <summary>
	/// Gets the session logger used for diagnostic output.
	/// </summary>
	/// <value>The <see cref="Logger"/> instance associated with this session.</value>
	internal Logger Logger { get; private set; }

	/// <summary>
	/// Gets or sets a value indicating whether the WinSCP child process is guarded by a Windows Job Object.
	/// </summary>
	/// <value><c>true</c> to guard the process; otherwise, <c>false</c>.</value>
	internal bool GuardProcessWithJobInternal
	{
		get
		{
			return _guardProcessWithJob;
		}
		set
		{
			CheckNotOpened();
			_guardProcessWithJob = value;
		}
	}

	/// <summary>
	/// Gets or sets a value used in tests to simulate closed handles.
	/// </summary>
	/// <value><c>true</c> to enable closed-handle simulation in tests; otherwise, <c>false</c>.</value>
	internal bool TestHandlesClosedInternal { get; set; }

	/// <summary>
	/// Gets the dictionary of raw WinSCP configuration settings added via <see cref="AddRawConfiguration"/>.
	/// </summary>
	/// <value>A dictionary mapping raw setting names to their values.</value>
	internal Dictionary<string, string> RawConfiguration { get; private set; }

	/// <summary>
	/// Gets a value indicating whether the default WinSCP configuration is used.
	/// </summary>
	/// <value><c>true</c> if the default configuration is active; otherwise, <c>false</c>.</value>
	internal bool DefaultConfigurationInternal => _defaultConfiguration;

	/// <summary>
	/// Gets the path to the INI file used for configuration, if any.
	/// </summary>
	/// <value>The INI file path, or <c>null</c> if not configured.</value>
	internal string IniFilePathInternal => _iniFilePath;

	/// <summary>
	/// Occurs when a file is transferred.
	/// </summary>
	public event FileTransferredEventHandler FileTransferred;

	/// <summary>
	/// Occurs when a failure happens in the session.
	/// </summary>
	public event FailedEventHandler Failed;

	/// <summary>
	/// Occurs when output data is received.
	/// </summary>
	public event OutputDataReceivedEventHandler OutputDataReceived;

	/// <summary>
	/// Occurs when file transfer progress is reported.
	/// </summary>
	public event FileTransferProgressEventHandler FileTransferProgress
	{
		add
		{
			using (CreateCallstackAndLock())
			{
				CheckNotOpened();
				_fileTransferProgress = (FileTransferProgressEventHandler)Delegate.Combine(_fileTransferProgress, value);
			}
		}
		remove
		{
			using (CreateCallstackAndLock())
			{
				CheckNotOpened();
				_fileTransferProgress = (FileTransferProgressEventHandler)Delegate.Remove(_fileTransferProgress, value);
			}
		}
	}

	/// <summary>
	/// Occurs when a query is received.
	/// </summary>
	public event QueryReceivedEventHandler QueryReceived
	{
		add
		{
			AddQueryReceived(value);
		}
		remove
		{
			RemoveQueryReceived(value);
		}
	}

	/// <summary>
	/// Creates a callstack log scope combined with the session lock, and verifies that no stream is
	/// currently being read from <see cref="GetFile"/>.
	/// </summary>
	/// <returns>A <see cref="CallstackAndLock"/> that must be disposed to release the lock.</returns>
	/// <exception cref="InvalidOperationException">Thrown when a <see cref="GetFile"/> stream is still open.</exception>
	private CallstackAndLock CreateCallstackAndLock()
	{
		CallstackAndLock callstackAndLock = Logger.CreateCallstackAndLock();
		if (_process != null && _process.StdOut != null)
		{
			callstackAndLock.Dispose();
			throw Logger.WriteException(new InvalidOperationException("Finish reading the stream from Session.GetFile first."));
		}
		return callstackAndLock;
	}

	/// <summary>
	/// Subscribes a handler to the <see cref="QueryReceived"/> event and, if the session is already
	/// open and this is the first subscriber, sends the appropriate option-batch command.
	/// </summary>
	/// <param name="value">The event handler to add.</param>
	private void AddQueryReceived(QueryReceivedEventHandler value)
	{
		using (CreateCallstackAndLock())
		{
			bool flag = _queryReceived == null;
			_queryReceived = (QueryReceivedEventHandler)Delegate.Combine(_queryReceived, value);
			if (Opened && flag)
			{
				SendOptionBatchCommand();
				WaitForGroup();
			}
		}
	}

	/// <summary>
	/// Unsubscribes a handler from the <see cref="QueryReceived"/> event and, if the session is open
	/// and no subscribers remain, restores the default batch-mode option.
	/// </summary>
	/// <param name="value">The event handler to remove.</param>
	private void RemoveQueryReceived(QueryReceivedEventHandler value)
	{
		using (CreateCallstackAndLock())
		{
			if (_queryReceived != null)
			{
				_queryReceived = (QueryReceivedEventHandler)Delegate.Remove(_queryReceived, value);
				if (Opened && _queryReceived == null)
				{
					SendOptionBatchCommand();
					WaitForGroup();
				}
			}
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Session"/> class.
	/// </summary>
	public Session()
	{
		Logger = new Logger();
		using (CreateCallstackAndLock())
		{
			Timeout = new TimeSpan(0, 1, 0);
			_reconnectTime = new TimeSpan(0, 2, 0);
			ResetOutput();
			_operationResults = new List<OperationResultBase>();
			_events = new List<Action>();
			_eventsEvent = new AutoResetEvent(initialState: false);
			_choiceEvent = new ManualResetEvent(initialState: false);
			_disposed = false;
			_defaultConfiguration = true;
			_logUnique = 0;
			_guardProcessWithJob = true;
			RawConfiguration = new Dictionary<string, string>();
		}
	}

	/// <summary>
	/// Clears the session output and error string collections.
	/// </summary>
	private void ResetOutput()
	{
		Output = new StringCollection();
		_error = new StringCollection();
	}

	/// <summary>
	/// Releases all resources used by the <see cref="Session"/>.
	/// </summary>
	public void Dispose()
	{
		using (CreateCallstackAndLock())
		{
			_disposed = true;
			Cleanup();
			Logger.Dispose();
			if (_eventsEvent != null)
			{
				_eventsEvent.Close();
				_eventsEvent = null;
			}
			if (_choiceEvent != null)
			{
				_choiceEvent.Close();
				_choiceEvent = null;
			}
			GC.SuppressFinalize(this);
		}
	}

	/// <summary>
	/// Aborts the session and terminates the process.
	/// </summary>
	public void Abort()
	{
		using (Logger.CreateCallstack())
		{
			CheckOpened();
			_aborted = true;
			_process?.Abort();
		}
	}

	/// <summary>
	/// Opens the session with the specified options.
	/// </summary>
	/// <param name="sessionOptions">The session options to use.</param>
	public void Open(SessionOptions sessionOptions)
	{
		using (CreateCallstackAndLock())
		{
			CheckNotOpened();
			if (sessionOptions == null)
			{
				throw Logger.WriteException(new ArgumentNullException("sessionOptions"));
			}
			try
			{
				SetupTempPath();
				ResetOutput();
				_process = ExeSessionProcess.CreateForSession(this);
				_process.OutputDataReceived += ProcessOutputDataReceived;
				_process.Start();
				GotOutput();
				SendOptionBatchCommand();
				WriteCommand("option confirm off");
				object obj = ((!(ReconnectTime != TimeSpan.MaxValue)) ? "off" : ((object)(int)ReconnectTime.TotalSeconds));
				string command = string.Format(CultureInfo.InvariantCulture, "option reconnecttime {0}", new object[1] { obj });
				WriteCommand(command);
				SessionOptionsToUrlAndSwitches(sessionOptions, scanFingerprint: false, out var command2, out var log);
				command2 = "open " + command2;
				log = "open " + log;
				WriteCommand(command2, log);
				Logger.WriteLine("Waiting for XML log file");
				do
				{
					string text;
					lock (Output)
					{
						text = ((_error.Count > 0) ? GetErrorOutputMessage() : ((Output.Count <= 0) ? "There was no output. " : string.Format(CultureInfo.CurrentCulture, "Output was \"{0}\". ", new object[1] { ListToString(Output) })));
					}
					text += string.Format(CultureInfo.CurrentCulture, "Response log file {0} was not created. This could indicate lack of write permissions to the log folder or problems starting WinSCP itself.", new object[1] { XmlLogPath });
					if (_process.HasExited && !File.Exists(XmlLogPath))
					{
						Logger.WriteCounters();
						Logger.WriteProcesses();
						_process.WriteStatus();
						string text2 = string.Format(CultureInfo.CurrentCulture, "{0}", new object[1] { _process.ExitCode });
						if (_process.ExitCode < 0)
						{
							text2 = string.Format(CultureInfo.CurrentCulture, "{0} ({1:X})", new object[2] { text2, _process.ExitCode });
						}
						throw Logger.WriteException(new SessionLocalException(this, string.Format(CultureInfo.CurrentCulture, "WinSCP process terminated with exit code {0}. ", new object[1] { text2 }) + text));
					}
					Thread.Sleep(50);
					CheckForTimeout("WinSCP has not responded in time. " + text);
				}
				while (!File.Exists(XmlLogPath));
				Logger.WriteLine("XML log file created");
				_logReader = new SessionLogReader(this);
				_logReader.WaitForNonEmptyElement("session", LogReadFlags.ThrowFailures);
				_reader = new SessionElementLogReader(_logReader);
				WaitForGroup();
				WriteCommand("pwd");
				using (ElementLogReader elementLogReader = _reader.WaitForGroupAndCreateLogReader())
				{
					using ElementLogReader elementLogReader2 = elementLogReader.WaitForNonEmptyElementAndCreateLogReader("cwd", LogReadFlags.ThrowFailures);
					while (elementLogReader2.Read((LogReadFlags)0))
					{
						if (elementLogReader2.GetEmptyElementValue("cwd", out var value))
						{
							_homePath = value;
						}
					}
					elementLogReader.ReadToEnd(LogReadFlags.ThrowFailures);
				}
				_sessionTimeout = sessionOptions.Timeout;
			}
			catch (Exception ex)
			{
				Logger.WriteLine("Exception: {0}", ex);
				Cleanup();
				throw;
			}
		}
	}

	/// <summary>
	/// Sends the <c>option batch</c> command to WinSCP, enabling or disabling batch mode
	/// depending on whether a <see cref="QueryReceived"/> handler is registered.
	/// </summary>
	private void SendOptionBatchCommand()
	{
		string command = string.Format(CultureInfo.InvariantCulture, "option batch {0}", new object[1] { (_queryReceived != null) ? "off" : "on" });
		WriteCommand(command);
	}

	/// <summary>
	/// Waits for the next XML log group element and reads it to completion, throwing on any failures.
	/// </summary>
	private void WaitForGroup()
	{
		using ElementLogReader reader = _reader.WaitForGroupAndCreateLogReader();
		ReadElement(reader, LogReadFlags.ThrowFailures);
	}

	/// <summary>
	/// Builds a formatted string from the captured error output lines, or <c>null</c> if there is none.
	/// </summary>
	/// <returns>A message describing the error output, or <c>null</c> when no error output was captured.</returns>
	internal string GetErrorOutputMessage()
	{
		string result = null;
		if (_error.Count > 0)
		{
			result = string.Format(CultureInfo.CurrentCulture, "Error output was \"{0}\". ", new object[1] { ListToString(_error) });
		}
		return result;
	}

	/// <summary>
	/// Joins all items in a <see cref="StringCollection"/> into a single newline-separated string.
	/// </summary>
	/// <param name="list">The collection of strings to join.</param>
	/// <returns>A single string with each item separated by <see cref="Environment.NewLine"/>.</returns>
	private static string ListToString(StringCollection list)
	{
		string[] array = new string[list.Count];
		list.CopyTo(array, 0);
		return string.Join(Environment.NewLine, array);
	}

	/// <summary>
	/// Scans the fingerprint for the specified algorithm.
	/// </summary>
	/// <param name="sessionOptions">The session options to use.</param>
	/// <param name="algorithm">The algorithm to scan.</param>
	/// <returns>The fingerprint string.</returns>
	public string ScanFingerprint(SessionOptions sessionOptions, string algorithm)
	{
		using (CreateCallstackAndLock())
		{
			string text = NormalizeIdent(algorithm);
			if (string.IsNullOrEmpty(text))
			{
				throw Logger.WriteException(new ArgumentException("Algorithm cannot be empty", "algorithm"));
			}
			CheckNotOpened();
			string text2;
			try
			{
				ResetOutput();
				SessionOptionsToUrlAndSwitches(sessionOptions, scanFingerprint: true, out var command, out var _);
				string additionalArguments = "/fingerprintscan " + command;
				_process = ExeSessionProcess.CreateForConsole(this, additionalArguments);
				_process.OutputDataReceived += ProcessOutputDataReceived;
				_process.Start();
				GotOutput();
				while (!_process.HasExited)
				{
					Thread.Sleep(50);
					CheckForTimeout();
				}
				if (_process.ExitCode != 0)
				{
					throw Logger.WriteException(new SessionRemoteException(this, ListToString(Output)));
				}
				text2 = null;
				foreach (string item in Output)
				{
					int num = item.IndexOf(":", StringComparison.Ordinal);
					if (num < 0)
					{
						throw Logger.WriteException(new SessionLocalException(this, string.Format(CultureInfo.CurrentCulture, "Unexpected fingerprint scan result line '{0}'", new object[1] { item })));
					}
					string value = NormalizeIdent(item.Substring(0, num).Trim());
					if (text.Equals(value, StringComparison.OrdinalIgnoreCase))
					{
						text2 = item.Substring(num + 1).Trim();
						break;
					}
				}
				if (text2 == null)
				{
					throw Logger.WriteException(new SessionLocalException(this, string.Format(CultureInfo.CurrentCulture, "Fingerprint for algorithm {0} not supported", new object[1] { algorithm })));
				}
			}
			catch (Exception ex)
			{
				Logger.WriteLine("Exception: {0}", ex);
				throw;
			}
			finally
			{
				Cleanup();
			}
			return text2;
		}
	}

	/// <summary>
	/// Normalizes an algorithm identifier string by removing all hyphen characters.
	/// </summary>
	/// <param name="algorithm">The algorithm identifier to normalize (e.g., "sha-256").</param>
	/// <returns>The identifier with hyphens removed (e.g., "sha256").</returns>
	private static string NormalizeIdent(string algorithm)
	{
		return algorithm.Replace("-", string.Empty);
	}

	/// <summary>
	/// Closes the session and releases resources.
	/// </summary>
	public void Close()
	{
		using (CreateCallstackAndLock())
		{
			CheckOpened();
			Cleanup();
		}
	}

	/// <summary>
	/// Lists the directory contents at the specified path.
	/// </summary>
	/// <param name="path">The remote path to list.</param>
	/// <returns>A <see cref="RemoteDirectoryInfo"/> object containing the directory contents.</returns>
	public RemoteDirectoryInfo ListDirectory(string path)
	{
		using (CreateCallstackAndLock())
		{
			CheckOpened();
			WriteCommand(string.Format(CultureInfo.InvariantCulture, "ls -- \"{0}\"", new object[1] { Tools.ArgumentEscape(IncludeTrailingSlash(path)) }));
			RemoteDirectoryInfo remoteDirectoryInfo = new RemoteDirectoryInfo();
			using (ElementLogReader elementLogReader = _reader.WaitForGroupAndCreateLogReader())
			{
				using ElementLogReader elementLogReader2 = elementLogReader.WaitForNonEmptyElementAndCreateLogReader("ls", LogReadFlags.ThrowFailures);
				string value = null;
				if (elementLogReader2.TryWaitForEmptyElement("destination", (LogReadFlags)0))
				{
					elementLogReader2.GetEmptyElementValue("destination", out value);
				}
				if (value == null || !elementLogReader2.TryWaitForNonEmptyElement("files", (LogReadFlags)0))
				{
					elementLogReader.ReadToEnd(LogReadFlags.ThrowFailures);
					throw Logger.WriteException(SessionLocalException.CreateElementNotFound(this, "files"));
				}
				value = IncludeTrailingSlash(value);
				using (ElementLogReader elementLogReader3 = elementLogReader2.CreateLogReader())
				{
					while (elementLogReader3.TryWaitForNonEmptyElement("file", (LogReadFlags)0))
					{
						RemoteFileInfo remoteFileInfo = new RemoteFileInfo();
						using ElementLogReader elementLogReader4 = elementLogReader3.CreateLogReader();
						while (elementLogReader4.Read((LogReadFlags)0))
						{
							if (elementLogReader4.GetEmptyElementValue("filename", out var value2))
							{
								remoteFileInfo.Name = value2;
								remoteFileInfo.FullName = value + value2;
							}
							else
							{
								ReadFile(remoteFileInfo, elementLogReader4);
							}
						}
						remoteDirectoryInfo.AddFile(remoteFileInfo);
					}
				}
				elementLogReader2.ReadToEnd(LogReadFlags.ThrowFailures);
				elementLogReader.ReadToEnd(LogReadFlags.ThrowFailures);
			}
			return remoteDirectoryInfo;
		}
	}

	/// <summary>
	/// Recursively enumerates remote files matching the given regular expression and enumeration options.
	/// </summary>
	/// <param name="path">The remote directory path to enumerate.</param>
	/// <param name="regex">The compiled regular expression used to filter file names.</param>
	/// <param name="options">Enumeration options controlling recursion, directory matching, and directory enumeration.</param>
	/// <param name="throwReadErrors">
	/// <c>true</c> to propagate <see cref="SessionRemoteException"/> when listing fails;
	/// <c>false</c> to silently skip unreadable directories.
	/// </param>
	/// <returns>A lazily evaluated sequence of matching <see cref="RemoteFileInfo"/> objects.</returns>
	/// <exception cref="ArgumentException">Thrown when incompatible enumeration options are combined.</exception>
	private IEnumerable<RemoteFileInfo> DoEnumerateRemoteFiles(string path, Regex regex, EnumerationOptions options, bool throwReadErrors)
	{
		Logger.WriteLine("Starting enumeration of {0} ...", path);
		bool allDirectories = (options & EnumerationOptions.AllDirectories) == EnumerationOptions.AllDirectories;
		bool matchDirectories = (options & EnumerationOptions.MatchDirectories) == EnumerationOptions.MatchDirectories;
		bool enumerateDirectories = (options & EnumerationOptions.EnumerateDirectories) == EnumerationOptions.EnumerateDirectories;
		if (enumerateDirectories && !allDirectories)
		{
			throw Logger.WriteException(new ArgumentException("Cannot use enumeration option EnumerateDirectories without AllDirectories"));
		}
		if (enumerateDirectories && matchDirectories)
		{
			throw Logger.WriteException(new ArgumentException("Cannot combine enumeration option EnumerateDirectories with MatchDirectories"));
		}
		RemoteDirectoryInfo remoteDirectoryInfo;
		try
		{
			remoteDirectoryInfo = ListDirectory(path);
		}
		catch (SessionRemoteException)
		{
			if (throwReadErrors)
			{
				throw;
			}
			remoteDirectoryInfo = null;
		}
		if (remoteDirectoryInfo != null)
		{
			foreach (RemoteFileInfo fileInfo in remoteDirectoryInfo.Files)
			{
				if (fileInfo.IsThisDirectory || fileInfo.IsParentDirectory)
				{
					continue;
				}
				bool flag = regex.IsMatch(fileInfo.Name);
				if ((!fileInfo.IsDirectory) ? flag : (enumerateDirectories || (matchDirectories && flag)))
				{
					Logger.WriteLine("Enumerating {0}", fileInfo.FullName);
					yield return fileInfo;
				}
				if (!(fileInfo.IsDirectory && allDirectories))
				{
					continue;
				}
				foreach (RemoteFileInfo item in DoEnumerateRemoteFiles(RemotePath.Combine(path, fileInfo.Name), regex, options, throwReadErrors: false))
				{
					yield return item;
				}
			}
		}
		Logger.WriteLine("Ended enumeration of {0}", path);
	}

	/// <summary>
	/// Enumerates remote files matching the specified mask and options.
	/// </summary>
	/// <param name="path">The remote path to enumerate.</param>
	/// <param name="mask">The file mask to match.</param>
	/// <param name="options">Enumeration options.</param>
	/// <returns>An enumerable of <see cref="RemoteFileInfo"/> objects.</returns>
	public IEnumerable<RemoteFileInfo> EnumerateRemoteFiles(string path, string mask, EnumerationOptions options)
	{
		using (CreateCallstackAndLock())
		{
			CheckOpened();
			Regex regex = MaskToRegex(mask);
			return new ImplicitEnumerable<RemoteFileInfo>(DoEnumerateRemoteFiles(path, regex, options, throwReadErrors: true));
		}
	}

	/// <summary>
	/// Converts a file mask containing wildcards (<c>*</c>, <c>?</c>) into a case-insensitive <see cref="Regex"/>.
	/// </summary>
	/// <param name="mask">The file mask to convert. <c>null</c>, empty, or <c>"*.*"</c> are treated as <c>"*"</c>.</param>
	/// <returns>A compiled, case-insensitive <see cref="Regex"/> equivalent to the mask.</returns>
	internal static Regex MaskToRegex(string mask)
	{
		if (string.IsNullOrEmpty(mask) || mask == "*.*")
		{
			mask = "*";
		}
		string text = "^";
		string text2 = mask;
		foreach (char c in text2)
		{
			text += c switch
			{
				'.' => "[.]", 
				'*' => ".*", 
				'?' => ".", 
				_ => Regex.Escape(new string(c, 1)), 
			};
		}
		text += "$";
		return new Regex(text, RegexOptions.IgnoreCase);
	}

	/// <summary>
	/// Uploads files to the remote path.
	/// </summary>
	/// <param name="localPath">The local path of the files.</param>
	/// <param name="remotePath">The remote path to upload to.</param>
	/// <param name="remove">Whether to remove files after upload.</param>
	/// <param name="options">Transfer options.</param>
	/// <returns>A <see cref="TransferOperationResult"/> representing the result.</returns>
	public TransferOperationResult PutFiles(string localPath, string remotePath, bool remove = false, TransferOptions options = null)
	{
		using (CreateCallstackAndLock())
		{
			return DoPutFiles(localPath, remotePath, remove, options);
		}
	}

	/// <summary>
	/// Core implementation of <see cref="PutFiles"/> that issues the WinSCP <c>put</c> command and
	/// reads the resulting XML log entries into a <see cref="TransferOperationResult"/>.
	/// </summary>
	/// <param name="localPath">The local file path or mask to upload.</param>
	/// <param name="remotePath">The remote destination path.</param>
	/// <param name="remove">Whether to delete local files after successful upload.</param>
	/// <param name="options">Transfer options; a default instance is created when <c>null</c>.</param>
	/// <returns>A <see cref="TransferOperationResult"/> describing the completed transfers.</returns>
	public TransferOperationResult DoPutFiles(string localPath, string remotePath, bool remove, TransferOptions options)
	{
		using (Logger.CreateCallstack())
		{
			if (options == null)
			{
				options = new TransferOptions();
			}
			CheckOpened();
			WriteCommand(string.Format(CultureInfo.InvariantCulture, "put {0} {1} -- \"{2}\" \"{3}\"", BooleanSwitch(remove, "delete"), options.ToSwitches(), Tools.ArgumentEscape(localPath), Tools.ArgumentEscape(remotePath)));
			TransferOperationResult transferOperationResult = new TransferOperationResult();
			using (ElementLogReader elementLogReader = _reader.WaitForGroupAndCreateLogReader())
			{
				using (RegisterOperationResult(transferOperationResult))
				{
					using (CreateProgressHandler())
					{
						TransferEventArgs e = null;
						bool flag = false;
						while (elementLogReader.Read((LogReadFlags)0))
						{
							if (elementLogReader.IsNonEmptyElement("upload"))
							{
								AddTransfer(transferOperationResult, e);
								e = TransferEventArgs.Read(ProgressSide.Local, elementLogReader);
								flag = false;
							}
							else if (elementLogReader.IsNonEmptyElement("mkdir"))
							{
								AddTransfer(transferOperationResult, e);
								e = null;
								flag = true;
							}
							else if (elementLogReader.IsNonEmptyElement("chmod"))
							{
								if (!flag)
								{
									if (e == null)
									{
										throw Logger.WriteException(new InvalidOperationException("Tag chmod before tag upload"));
									}
									e.Chmod = ChmodEventArgs.Read(elementLogReader);
								}
							}
							else if (elementLogReader.IsNonEmptyElement("touch") && !flag)
							{
								if (e == null)
								{
									throw Logger.WriteException(new InvalidOperationException("Tag touch before tag upload"));
								}
								e.Touch = TouchEventArgs.Read(elementLogReader);
							}
						}
						AddTransfer(transferOperationResult, e);
					}
				}
			}
			return transferOperationResult;
		}
	}

	/// <summary>
	/// Uploads files to a remote directory.
	/// </summary>
	/// <param name="localDirectory">The local directory.</param>
	/// <param name="remoteDirectory">The remote directory.</param>
	/// <param name="filemask">The file mask to match.</param>
	/// <param name="remove">Whether to remove files after upload.</param>
	/// <param name="options">Transfer options.</param>
	/// <returns>A <see cref="TransferOperationResult"/> representing the result.</returns>
	public TransferOperationResult PutFilesToDirectory(string localDirectory, string remoteDirectory, string filemask = null, bool remove = false, TransferOptions options = null)
	{
		using (Logger.CreateCallstack())
		{
			if (localDirectory == null)
			{
				throw Logger.WriteException(new ArgumentNullException("localDirectory"));
			}
			if (remoteDirectory == null)
			{
				throw Logger.WriteException(new ArgumentNullException("remoteDirectory"));
			}
			if (string.IsNullOrEmpty(filemask))
			{
				filemask = "*";
			}
			string localPath = Path.Combine(localDirectory, filemask);
			string remotePath = RemotePath.AddDirectorySeparator(remoteDirectory);
			return PutFiles(localPath, remotePath, remove, options);
		}
	}

	/// <summary>
	/// Uploads a single file to a remote directory.
	/// </summary>
	/// <param name="localFilePath">The local file path.</param>
	/// <param name="remoteDirectory">The remote directory.</param>
	/// <param name="remove">Whether to remove the file after upload.</param>
	/// <param name="options">Transfer options.</param>
	/// <returns>A <see cref="TransferEventArgs"/> representing the result.</returns>
	public TransferEventArgs PutFileToDirectory(string localFilePath, string remoteDirectory, bool remove = false, TransferOptions options = null)
	{
		using (Logger.CreateCallstack())
		{
			if (!File.Exists(localFilePath))
			{
				throw Logger.WriteException(new FileNotFoundException("File " + localFilePath + " does not exist", localFilePath));
			}
			TransferOperationResult transferOperationResult = PutEntryToDirectory(localFilePath, remoteDirectory, remove, options);
			return GetOnlyFileOperation(transferOperationResult.Transfers);
		}
	}

	/// <summary>
	/// Uploads a file from a stream to the remote path.
	/// </summary>
	/// <param name="stream">The stream containing the file data.</param>
	/// <param name="remoteFilePath">The remote file path.</param>
	/// <param name="options">Transfer options.</param>
	public void PutFile(Stream stream, string remoteFilePath, TransferOptions options = null)
	{
		using (CreateCallstackAndLock())
		{
			if (remoteFilePath == null)
			{
				throw Logger.WriteException(new ArgumentNullException("remoteFilePath"));
			}
			if (_process.StdIn != null)
			{
				throw Logger.WriteException(new InvalidOperationException("Already uploading from a stream"));
			}
			_process.StdIn = stream ?? throw Logger.WriteException(new ArgumentNullException("stream"));
			try
			{
				remoteFilePath = RemotePath.EscapeOperationMask(remoteFilePath);
				TransferOperationResult transferOperationResult = DoPutFiles("-", remoteFilePath, remove: false, options);
				transferOperationResult.Check();
				GetOnlyFileOperation(transferOperationResult.Transfers);
			}
			finally
			{
				_process.StdIn = null;
			}
		}
	}

	/// <summary>
	/// Uploads a single local file or directory entry to the specified remote directory and
	/// verifies the result, throwing on failure.
	/// </summary>
	/// <param name="localFilePath">The full path of the local file to upload. Must not be empty.</param>
	/// <param name="remoteDirectory">The remote destination directory.</param>
	/// <param name="remove">Whether to delete the local file after a successful upload.</param>
	/// <param name="options">Transfer options; a default instance is used when <c>null</c>.</param>
	/// <returns>A checked <see cref="TransferOperationResult"/>.</returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="localFilePath"/> is null or empty.</exception>
	internal TransferOperationResult PutEntryToDirectory(string localFilePath, string remoteDirectory, bool remove = false, TransferOptions options = null)
	{
		using (Logger.CreateCallstack())
		{
			if (string.IsNullOrEmpty(localFilePath))
			{
				throw Logger.WriteException(new ArgumentException("File to path cannot be empty", "localFilePath"));
			}
			string directoryName = Path.GetDirectoryName(localFilePath);
			string fileName = Path.GetFileName(localFilePath);
			TransferOperationResult transferOperationResult = PutFilesToDirectory(directoryName, remoteDirectory, fileName, remove, options);
			transferOperationResult.Check();
			return transferOperationResult;
		}
	}

	/// <summary>
	/// Adds a completed transfer to the operation result and raises the <see cref="FileTransferred"/> event.
	/// </summary>
	/// <param name="result">The operation result to update.</param>
	/// <param name="args">The transfer event arguments; no action is taken when <c>null</c>.</param>
	private void AddTransfer(TransferOperationResult result, TransferEventArgs args)
	{
		if (args != null)
		{
			result.AddTransfer(args);
			RaiseFileTransferredEvent(args);
		}
	}

	/// <summary>
	/// Downloads files from the remote path.
	/// </summary>
	/// <param name="remotePath">The remote path.</param>
	/// <param name="localPath">The local path to download to.</param>
	/// <param name="remove">Whether to remove files after download.</param>
	/// <param name="options">Transfer options.</param>
	/// <returns>A <see cref="TransferOperationResult"/> representing the result.</returns>
	public TransferOperationResult GetFiles(string remotePath, string localPath, bool remove = false, TransferOptions options = null)
	{
		using (CreateCallstackAndLock())
		{
			return DoGetFiles(remotePath, localPath, remove, options, string.Empty);
		}
	}

	/// <summary>
	/// Core implementation that issues the WinSCP <c>get</c> command with optional extra parameters and
	/// collects the resulting XML log entries into a <see cref="TransferOperationResult"/>.
	/// </summary>
	/// <param name="remotePath">The remote path or mask to download.</param>
	/// <param name="localPath">The local destination path.</param>
	/// <param name="remove">Whether to delete remote files after a successful download.</param>
	/// <param name="options">Transfer options; a default instance is used when <c>null</c>.</param>
	/// <param name="additionalParams">Extra command-line switches appended to the <c>get</c> command.</param>
	/// <returns>A <see cref="TransferOperationResult"/> describing the completed transfers.</returns>
	private TransferOperationResult DoGetFiles(string remotePath, string localPath, bool remove, TransferOptions options, string additionalParams)
	{
		using (Logger.CreateCallstack())
		{
			StartGetCommand(remotePath, localPath, remove, options, additionalParams);
			TransferOperationResult transferOperationResult = new TransferOperationResult();
			using (ElementLogReader elementLogReader = _reader.WaitForGroupAndCreateLogReader())
			{
				using (RegisterOperationResult(transferOperationResult))
				{
					using (CreateProgressHandler())
					{
						TransferEventArgs e = null;
						while (elementLogReader.Read((LogReadFlags)0))
						{
							if (elementLogReader.IsNonEmptyElement("download"))
							{
								AddTransfer(transferOperationResult, e);
								e = TransferEventArgs.Read(ProgressSide.Remote, elementLogReader);
							}
							else if (elementLogReader.IsNonEmptyElement("rm") && e != null && e.Removal == null)
							{
								e.Removal = RemovalEventArgs.Read(elementLogReader);
							}
						}
						AddTransfer(transferOperationResult, e);
					}
				}
			}
			return transferOperationResult;
		}
	}

	/// <summary>
	/// Validates that the session is open, builds, and sends the WinSCP <c>get</c> command.
	/// </summary>
	/// <param name="remotePath">The remote path or mask to download.</param>
	/// <param name="localPath">The local destination path.</param>
	/// <param name="remove">Whether to delete remote files after a successful download.</param>
	/// <param name="options">Transfer options; a default instance is used when <c>null</c>.</param>
	/// <param name="additionalParams">Extra command-line switches inserted before the path arguments.</param>
	private void StartGetCommand(string remotePath, string localPath, bool remove, TransferOptions options, string additionalParams)
	{
		if (options == null)
		{
			options = new TransferOptions();
		}
		CheckOpened();
		WriteCommand(string.Format(CultureInfo.InvariantCulture, "get {0} {1} {2} -- \"{3}\" \"{4}\"", BooleanSwitch(remove, "delete"), options.ToSwitches(), additionalParams, Tools.ArgumentEscape(remotePath), Tools.ArgumentEscape(localPath)));
	}

	/// <summary>
	/// Downloads files from a remote directory to a local directory.
	/// </summary>
	/// <param name="remoteDirectory">The remote directory.</param>
	/// <param name="localDirectory">The local directory.</param>
	/// <param name="filemask">The file mask to match.</param>
	/// <param name="remove">Whether to remove files after download.</param>
	/// <param name="options">Transfer options.</param>
	/// <returns>A <see cref="TransferOperationResult"/> representing the result.</returns>
	public TransferOperationResult GetFilesToDirectory(string remoteDirectory, string localDirectory, string filemask = null, bool remove = false, TransferOptions options = null)
	{
		using (CreateCallstackAndLock())
		{
			return DoGetFilesToDirectory(remoteDirectory, localDirectory, filemask, remove, options, null);
		}
	}

	/// <summary>
	/// Core implementation that validates arguments, constructs the remote path, and delegates to
	/// <see cref="DoGetFiles"/> to download files into a local directory.
	/// </summary>
	/// <param name="remoteDirectory">The remote directory to download from. Must not be <c>null</c>.</param>
	/// <param name="localDirectory">The local directory to download into. Must exist and must not be <c>null</c>.</param>
	/// <param name="filemask">The file mask to apply; defaults to <c>"*"</c> when null or empty.</param>
	/// <param name="remove">Whether to delete remote files after a successful download.</param>
	/// <param name="options">Transfer options; a default instance is used when <c>null</c>.</param>
	/// <param name="additionalParams">Extra command-line switches forwarded to the <c>get</c> command.</param>
	/// <returns>A <see cref="TransferOperationResult"/> describing the completed transfers.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="remoteDirectory"/> or <paramref name="localDirectory"/> is <c>null</c>.</exception>
	/// <exception cref="DirectoryNotFoundException">Thrown when <paramref name="localDirectory"/> does not exist.</exception>
	private TransferOperationResult DoGetFilesToDirectory(string remoteDirectory, string localDirectory, string filemask, bool remove, TransferOptions options, string additionalParams)
	{
		using (Logger.CreateCallstack())
		{
			if (remoteDirectory == null)
			{
				throw Logger.WriteException(new ArgumentNullException("remoteDirectory"));
			}
			if (localDirectory == null)
			{
				throw Logger.WriteException(new ArgumentNullException("localDirectory"));
			}
			if (string.IsNullOrEmpty(filemask))
			{
				filemask = "*";
			}
			string remotePath = RemotePath.Combine(remoteDirectory, filemask);
			if (!Directory.Exists(localDirectory))
			{
				throw Logger.WriteException(new DirectoryNotFoundException(localDirectory));
			}
			string text = Path.DirectorySeparatorChar.ToString();
			string localPath = localDirectory + (localDirectory.EndsWith(text, StringComparison.Ordinal) ? string.Empty : text);
			return DoGetFiles(remotePath, localPath, remove, options, additionalParams);
		}
	}

	/// <summary>
	/// Downloads a single file from a remote directory to a local directory.
	/// </summary>
	/// <param name="remoteFilePath">The remote file path.</param>
	/// <param name="localDirectory">The local directory.</param>
	/// <param name="remove">Whether to remove the file after download.</param>
	/// <param name="options">Transfer options.</param>
	/// <returns>A <see cref="TransferEventArgs"/> representing the result.</returns>
	public TransferEventArgs GetFileToDirectory(string remoteFilePath, string localDirectory, bool remove = false, TransferOptions options = null)
	{
		using (CreateCallstackAndLock())
		{
			TransferOperationResult entryToDirectory = GetEntryToDirectory(remoteFilePath, localDirectory, remove, options, "-onlyfile");
			return GetOnlyFileOperation(entryToDirectory.Transfers);
		}
	}

	/// <summary>
	/// Downloads a single remote file or directory entry to the specified local directory and
	/// verifies the result, throwing on failure.
	/// </summary>
	/// <param name="remoteFilePath">The full remote file path to download. Must not be empty.</param>
	/// <param name="localDirectory">The local directory to download into.</param>
	/// <param name="remove">Whether to delete the remote file after a successful download.</param>
	/// <param name="options">Transfer options; a default instance is used when <c>null</c>.</param>
	/// <param name="additionalParams">Extra command-line switches forwarded to the <c>get</c> command, or <c>null</c>.</param>
	/// <returns>A checked <see cref="TransferOperationResult"/>.</returns>
	internal TransferOperationResult GetEntryToDirectory(string remoteFilePath, string localDirectory, bool remove = false, TransferOptions options = null, string additionalParams = null)
	{
		using (Logger.CreateCallstack())
		{
			ParseRemotePath(remoteFilePath, out var remoteDirectory, out var filemask);
			TransferOperationResult transferOperationResult = DoGetFilesToDirectory(remoteDirectory, localDirectory, filemask, remove, options, additionalParams ?? string.Empty);
			transferOperationResult.Check();
			return transferOperationResult;
		}
	}

	/// <summary>
	/// Splits a full remote file path into its directory component and an escaped file mask.
	/// </summary>
	/// <param name="remoteFilePath">The full remote file path to parse. Must not be null or empty.</param>
	/// <param name="remoteDirectory">Output: the directory portion of the path.</param>
	/// <param name="filemask">Output: the file-name portion, escaped for use as a file mask.</param>
	/// <exception cref="ArgumentException">Thrown when <paramref name="remoteFilePath"/> is null or empty.</exception>
	private void ParseRemotePath(string remoteFilePath, out string remoteDirectory, out string filemask)
	{
		if (string.IsNullOrEmpty(remoteFilePath))
		{
			throw Logger.WriteException(new ArgumentException("File to path cannot be empty", "remoteFilePath"));
		}
		remoteDirectory = RemotePath.GetDirectoryName(remoteFilePath);
		filemask = RemotePath.EscapeFileMask(RemotePath.GetFileName(remoteFilePath));
	}

	/// <summary>
	/// Returns the single element from a collection of file operations, throwing when the collection
	/// has zero or more than one element.
	/// </summary>
	/// <typeparam name="T">The type of the operation result.</typeparam>
	/// <param name="operations">The collection of operations expected to contain exactly one item.</param>
	/// <returns>The sole element in <paramref name="operations"/>.</returns>
	/// <exception cref="FileNotFoundException">Thrown when <paramref name="operations"/> is empty.</exception>
	/// <exception cref="InvalidOperationException">Thrown when <paramref name="operations"/> contains more than one element.</exception>
	private T GetOnlyFileOperation<T>(ICollection<T> operations)
	{
		if (operations.Count == 0)
		{
			throw Logger.WriteException(new FileNotFoundException("File not found"));
		}
		if (operations.Count > 1)
		{
			throw Logger.WriteException(new InvalidOperationException("More then one file has been unexpectedly found"));
		}
		return operations.First();
	}

	/// <summary>
	/// Gets a file as a stream from the remote path.
	/// </summary>
	/// <param name="remoteFilePath">The remote file path.</param>
	/// <param name="options">Transfer options.</param>
	/// <returns>A <see cref="Stream"/> for the file data.</returns>
	public Stream GetFile(string remoteFilePath, TransferOptions options = null)
	{
		TransferOperationResult result;
		ElementLogReader groupReader;
		IDisposable operationResultGuard;
		IDisposable progressHandler;
		using (CreateCallstackAndLock())
		{
			ParseRemotePath(remoteFilePath, out var remoteDirectory, out var filemask);
			string remotePath = RemotePath.Combine(remoteDirectory, filemask);
			result = new TransferOperationResult();
			groupReader = null;
			operationResultGuard = null;
			progressHandler = null;
			if (_process.StdOut != null)
			{
				throw Logger.WriteException(new InvalidOperationException("Data stream already exist"));
			}
			PipeStream pipeStream = new PipeStream();
			_process.StdOut = pipeStream;
			try
			{
				StartGetCommand(remotePath, "-", remove: false, options, "-onlyfile");
				groupReader = _reader.WaitForGroupAndCreateLogReader();
				operationResultGuard = RegisterOperationResult(result);
				progressHandler = CreateProgressHandler();
				if (_throwStdOut)
				{
					throw Logger.WriteException(new InvalidOperationException());
				}
				_throwStdOut = true;
				bool flag;
				try
				{
					flag = groupReader.TryWaitForNonEmptyElement("download", (LogReadFlags)0);
				}
				catch (StdOutException)
				{
					flag = true;
				}
				finally
				{
					_throwStdOut = false;
				}
				if (flag)
				{
					Logger.WriteLine("Download stream started");
					pipeStream.OnDispose = onGetEndWithExit;
					return pipeStream;
				}
				result.Check();
				throw Logger.WriteException(new FileNotFoundException("File not found"));
			}
			catch
			{
				onGetEnd();
				throw;
			}
		}
		void onGetEnd()
		{
			using (Logger.CreateCallstack())
			{
				try
				{
					progressHandler?.Dispose();
				}
				finally
				{
					try
					{
						groupReader?.Dispose();
					}
					finally
					{
						_process.StdOut = null;
						operationResultGuard?.Dispose();
					}
				}
			}
		}
		void onGetEndWithExit()
		{
			using (Logger.CreateCallstackAndLock())
			{
				Logger.WriteLine("Closing download stream");
				try
				{
					onGetEnd();
				}
				finally
				{
					Logger.WriteLine("Closed download stream");
					result.Check();
				}
			}
		}
	}

	/// <summary>
	/// Removes files at the specified path.
	/// </summary>
	/// <param name="path">The path of the files to remove.</param>
	/// <returns>A <see cref="RemovalOperationResult"/> representing the result.</returns>
	public RemovalOperationResult RemoveFiles(string path)
	{
		using (CreateCallstackAndLock())
		{
			return DoRemoveFiles(path, string.Empty);
		}
	}

	/// <summary>
	/// Issues the WinSCP <c>rm</c> command and collects the resulting removal events.
	/// </summary>
	/// <param name="path">The remote path or mask of files to remove.</param>
	/// <param name="additionalParams">Extra command-line switches prepended to the path argument.</param>
	/// <returns>A <see cref="RemovalOperationResult"/> describing the removed files.</returns>
	private RemovalOperationResult DoRemoveFiles(string path, string additionalParams)
	{
		using (Logger.CreateCallstack())
		{
			CheckOpened();
			WriteCommand(string.Format(CultureInfo.InvariantCulture, "rm {0} -- \"{1}\"", new object[2]
			{
				additionalParams,
				Tools.ArgumentEscape(path)
			}));
			RemovalOperationResult removalOperationResult = new RemovalOperationResult();
			using (ElementLogReader elementLogReader = _reader.WaitForGroupAndCreateLogReader())
			{
				using (RegisterOperationResult(removalOperationResult))
				{
					while (elementLogReader.Read((LogReadFlags)0))
					{
						if (elementLogReader.IsNonEmptyElement("rm"))
						{
							removalOperationResult.AddRemoval(RemovalEventArgs.Read(elementLogReader));
						}
					}
				}
			}
			return removalOperationResult;
		}
	}

	/// <summary>
	/// Removes a single file at the specified path.
	/// </summary>
	/// <param name="path">The path of the file to remove.</param>
	/// <returns>A <see cref="RemovalEventArgs"/> representing the result.</returns>
	public RemovalEventArgs RemoveFile(string path)
	{
		using (CreateCallstackAndLock())
		{
			RemovalOperationResult removalOperationResult = RemoveEntry(path, "-onlyfile");
			return GetOnlyFileOperation(removalOperationResult.Removals);
		}
	}

	/// <summary>
	/// Validates the path, escapes the file mask component, and delegates to <see cref="DoRemoveFiles"/>,
	/// throwing on failure.
	/// </summary>
	/// <param name="path">The full remote file path to remove. Must not be null or empty.</param>
	/// <param name="additionalParams">Optional extra command-line switches forwarded to the <c>rm</c> command.</param>
	/// <returns>A checked <see cref="RemovalOperationResult"/>.</returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is null, empty, or has an empty file name component.</exception>
	internal RemovalOperationResult RemoveEntry(string path, string additionalParams = null)
	{
		using (Logger.CreateCallstack())
		{
			if (string.IsNullOrEmpty(path))
			{
				throw Logger.WriteException(new ArgumentException("File to path cannot be empty", "path"));
			}
			string directoryName = RemotePath.GetDirectoryName(path);
			string text = RemotePath.EscapeFileMask(RemotePath.GetFileName(path));
			if (string.IsNullOrEmpty(text))
			{
				throw Logger.WriteException(new ArgumentException("File name cannot be empty", "path"));
			}
			path = RemotePath.Combine(directoryName, text);
			RemovalOperationResult removalOperationResult = DoRemoveFiles(path, additionalParams ?? string.Empty);
			removalOperationResult.Check();
			return removalOperationResult;
		}
	}

	/// <summary>
	/// Synchronizes directories between local and remote paths.
	/// </summary>
	/// <param name="mode">The synchronization mode.</param>
	/// <param name="localPath">The local path.</param>
	/// <param name="remotePath">The remote path.</param>
	/// <param name="removeFiles">Whether to remove files during synchronization.</param>
	/// <param name="mirror">Whether to mirror files.</param>
	/// <param name="criteria">Synchronization criteria.</param>
	/// <param name="options">Transfer options.</param>
	/// <returns>A <see cref="SynchronizationResult"/> representing the result.</returns>
	public SynchronizationResult SynchronizeDirectories(SynchronizationMode mode, string localPath, string remotePath, bool removeFiles, bool mirror = false, SynchronizationCriteria criteria = SynchronizationCriteria.Time, TransferOptions options = null)
	{
		using (CreateCallstackAndLock())
		{
			DoSynchronizeDirectories(mode, localPath, remotePath, removeFiles, mirror, criteria, options, string.Empty);
			return ReadSynchronizeDirectories();
		}
	}

	/// <summary>
	/// Validates synchronization arguments and issues the WinSCP <c>synchronize</c> command.
	/// </summary>
	/// <param name="mode">The synchronization direction.</param>
	/// <param name="localPath">The local directory path.</param>
	/// <param name="remotePath">The remote directory path.</param>
	/// <param name="removeFiles">Whether to delete files not present in the source.</param>
	/// <param name="mirror">Whether to apply mirror semantics.</param>
	/// <param name="criteria">The criteria used to determine which files need synchronization.</param>
	/// <param name="options">Transfer options; a default instance is used when <c>null</c>.</param>
	/// <param name="additionalParameters">Extra command-line switches inserted into the command.</param>
	/// <exception cref="ArgumentException">Thrown when incompatible arguments are combined (e.g., delete with Both mode).</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="mode"/> or <paramref name="criteria"/> is out of range.</exception>
	private void DoSynchronizeDirectories(SynchronizationMode mode, string localPath, string remotePath, bool removeFiles, bool mirror, SynchronizationCriteria criteria, TransferOptions options, string additionalParameters)
	{
		if (options == null)
		{
			options = new TransferOptions();
		}
		CheckOpened();
		if (removeFiles && mode == SynchronizationMode.Both)
		{
			throw Logger.WriteException(new ArgumentException("Cannot delete files in synchronization mode Both"));
		}
		if (mirror && mode == SynchronizationMode.Both)
		{
			throw Logger.WriteException(new ArgumentException("Cannot mirror files in synchronization mode Both"));
		}
		if (criteria != SynchronizationCriteria.Time && mode == SynchronizationMode.Both)
		{
			throw Logger.WriteException(new ArgumentException("Only Time criteria is allowed in synchronization mode Both"));
		}
		string text = mode switch
		{
			SynchronizationMode.Local => "local", 
			SynchronizationMode.Remote => "remote", 
			SynchronizationMode.Both => "both", 
			_ => throw Logger.WriteException(new ArgumentOutOfRangeException("mode")), 
		};
		string text2;
		if (criteria == SynchronizationCriteria.None)
		{
			text2 = "none";
		}
		else
		{
			Dictionary<SynchronizationCriteria, string> obj = new Dictionary<SynchronizationCriteria, string>
			{
				{
					SynchronizationCriteria.Time,
					"time"
				},
				{
					SynchronizationCriteria.Size,
					"size"
				},
				{
					SynchronizationCriteria.Checksum,
					"checksum"
				}
			};
			SynchronizationCriteria synchronizationCriteria = criteria;
			text2 = string.Empty;
			foreach (KeyValuePair<SynchronizationCriteria, string> item in obj)
			{
				if (synchronizationCriteria.HasFlag(item.Key))
				{
					synchronizationCriteria -= item.Key;
					text2 = text2 + ((text2.Length > 0) ? "," : string.Empty) + item.Value;
				}
			}
			if (synchronizationCriteria != SynchronizationCriteria.None)
			{
				throw Logger.WriteException(new ArgumentOutOfRangeException("criteria"));
			}
		}
		WriteCommand(string.Format(CultureInfo.InvariantCulture, "synchronize {0} {1} {2} {3} -criteria=\"{4}\" {5} -- \"{6}\" \"{7}\"", text, BooleanSwitch(removeFiles, "delete"), BooleanSwitch(mirror, "mirror"), options.ToSwitches(), text2, additionalParameters, Tools.ArgumentEscape(localPath), Tools.ArgumentEscape(remotePath)));
	}

	/// <summary>
	/// Reads the XML log output of a <c>synchronize</c> command and populates a <see cref="SynchronizationResult"/>.
	/// </summary>
	/// <returns>A <see cref="SynchronizationResult"/> containing all uploads, downloads, and removals.</returns>
	private SynchronizationResult ReadSynchronizeDirectories()
	{
		using (Logger.CreateCallstack())
		{
			SynchronizationResult synchronizationResult = new SynchronizationResult();
			using (ElementLogReader elementLogReader = _reader.WaitForGroupAndCreateLogReader())
			{
				using (RegisterOperationResult(synchronizationResult))
				{
					using (CreateProgressHandler())
					{
						TransferEventArgs e = null;
						bool flag = false;
						while (elementLogReader.Read((LogReadFlags)0))
						{
							ProgressSide? progressSide = null;
							if (elementLogReader.IsNonEmptyElement("upload"))
							{
								progressSide = ProgressSide.Local;
							}
							else if (elementLogReader.IsNonEmptyElement("download"))
							{
								progressSide = ProgressSide.Remote;
							}
							if (progressSide.HasValue)
							{
								AddSynchronizationTransfer(synchronizationResult, e);
								e = TransferEventArgs.Read(progressSide.Value, elementLogReader);
								flag = false;
							}
							else if (elementLogReader.IsNonEmptyElement("rm"))
							{
								synchronizationResult.AddRemoval(RemovalEventArgs.Read(elementLogReader));
							}
							else if (elementLogReader.IsNonEmptyElement("mkdir"))
							{
								AddSynchronizationTransfer(synchronizationResult, e);
								e = null;
								flag = true;
							}
							else if (elementLogReader.IsNonEmptyElement("chmod"))
							{
								if (!flag)
								{
									if (e == null)
									{
										throw Logger.WriteException(new InvalidOperationException("Tag chmod before tag download"));
									}
									e.Chmod = ChmodEventArgs.Read(elementLogReader);
								}
							}
							else if (elementLogReader.IsNonEmptyElement("touch") && !flag)
							{
								if (e == null)
								{
									throw Logger.WriteException(new InvalidOperationException("Tag touch before tag download"));
								}
								e.Touch = TouchEventArgs.Read(elementLogReader);
							}
						}
						AddSynchronizationTransfer(synchronizationResult, e);
					}
				}
			}
			return synchronizationResult;
		}
	}

	/// <summary>
	/// Compares directories between local and remote paths.
	/// </summary>
	/// <param name="mode">The synchronization mode.</param>
	/// <param name="localPath">The local path.</param>
	/// <param name="remotePath">The remote path.</param>
	/// <param name="removeFiles">Whether to remove files during comparison.</param>
	/// <param name="mirror">Whether to mirror files.</param>
	/// <param name="criteria">Synchronization criteria.</param>
	/// <param name="options">Transfer options.</param>
	/// <returns>A <see cref="ComparisonDifferenceCollection"/> representing the result.</returns>
	public ComparisonDifferenceCollection CompareDirectories(SynchronizationMode mode, string localPath, string remotePath, bool removeFiles, bool mirror = false, SynchronizationCriteria criteria = SynchronizationCriteria.Time, TransferOptions options = null)
	{
		using (CreateCallstackAndLock())
		{
			DoSynchronizeDirectories(mode, localPath, remotePath, removeFiles, mirror, criteria, options, "-preview");
			return ReadCompareDirectories(localPath, remotePath);
		}
	}

	/// <summary>
	/// Reads the XML log output of a preview synchronize command and builds a
	/// <see cref="ComparisonDifferenceCollection"/> describing the detected differences.
	/// </summary>
	/// <param name="localPath">The local directory path used as the comparison base.</param>
	/// <param name="remotePath">The remote directory path used as the comparison base.</param>
	/// <returns>A <see cref="ComparisonDifferenceCollection"/> listing all directory differences.</returns>
	private ComparisonDifferenceCollection ReadCompareDirectories(string localPath, string remotePath)
	{
		using (Logger.CreateCallstack())
		{
			ComparisonDifferenceCollection comparisonDifferenceCollection = new ComparisonDifferenceCollection();
			using (ElementLogReader elementLogReader = _reader.WaitForGroupAndCreateLogReader())
			{
				while (elementLogReader.TryWaitForNonEmptyElement("difference", LogReadFlags.ThrowFailures))
				{
					using ElementLogReader elementLogReader2 = elementLogReader.CreateLogReader();
					ComparisonDifference comparisonDifference = new ComparisonDifference(localPath, remotePath);
					ComparisonFileInfo comparisonFileInfo = null;
					while (elementLogReader2.Read((LogReadFlags)0))
					{
						string value2;
						string value3;
						if (elementLogReader2.GetEmptyElementValue("action", out var value))
						{
							if (comparisonDifference.Local != null || comparisonDifference.Remote != null)
							{
								throw Logger.WriteException(new InvalidOperationException("Tag action after filename"));
							}
							if (value.Equals("uploadnew", StringComparison.OrdinalIgnoreCase))
							{
								comparisonDifference.Action = SynchronizationAction.UploadNew;
								comparisonDifference.Local = new ComparisonFileInfo();
								continue;
							}
							if (value.Equals("downloadnew", StringComparison.OrdinalIgnoreCase))
							{
								comparisonDifference.Action = SynchronizationAction.DownloadNew;
								comparisonDifference.Remote = new ComparisonFileInfo();
								continue;
							}
							if (value.Equals("uploadupdate", StringComparison.OrdinalIgnoreCase))
							{
								comparisonDifference.Action = SynchronizationAction.UploadUpdate;
								comparisonDifference.Local = new ComparisonFileInfo();
								comparisonDifference.Remote = new ComparisonFileInfo();
								continue;
							}
							if (value.Equals("downloadupdate", StringComparison.OrdinalIgnoreCase))
							{
								comparisonDifference.Action = SynchronizationAction.DownloadUpdate;
								comparisonDifference.Remote = new ComparisonFileInfo();
								comparisonDifference.Local = new ComparisonFileInfo();
								continue;
							}
							if (value.Equals("deleteremote", StringComparison.OrdinalIgnoreCase))
							{
								comparisonDifference.Action = SynchronizationAction.DeleteRemote;
								comparisonDifference.Remote = new ComparisonFileInfo();
								continue;
							}
							if (!value.Equals("deletelocal", StringComparison.OrdinalIgnoreCase))
							{
								throw Logger.WriteException(new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "Unknown synchronization action \"{0}\"", new object[1] { value })));
							}
							comparisonDifference.Action = SynchronizationAction.DeleteLocal;
							comparisonDifference.Local = new ComparisonFileInfo();
						}
						else if (elementLogReader2.GetEmptyElementValue("type", out value2))
						{
							comparisonDifference.IsDirectory = value2.Length == 1 && RemoteFileInfo.IsDirectoryFileType(value2[0]);
						}
						else if (elementLogReader2.GetEmptyElementValue("filename", out value3))
						{
							if (comparisonFileInfo == null)
							{
								comparisonFileInfo = comparisonDifference.Local ?? comparisonDifference.Remote;
							}
							else if (comparisonFileInfo == comparisonDifference.Local)
							{
								comparisonFileInfo = comparisonDifference.Remote;
							}
							if (comparisonFileInfo == null)
							{
								throw Logger.WriteException(new InvalidOperationException("Unexpected filename tag"));
							}
							comparisonFileInfo.FileName = value3;
						}
						else if (elementLogReader2.GetEmptyElementValue("modification", out value3))
						{
							if (comparisonFileInfo == null)
							{
								throw Logger.WriteException(new InvalidOperationException("Unexpected modification tag"));
							}
							comparisonFileInfo.LastWriteTime = XmlConvert.ToDateTime(value3, XmlDateTimeSerializationMode.Local);
						}
						else if (elementLogReader2.GetEmptyElementValue("size", out value3))
						{
							if (comparisonFileInfo == null)
							{
								throw Logger.WriteException(new InvalidOperationException("Unexpected size tag"));
							}
							comparisonFileInfo.Length = long.Parse(value3, CultureInfo.InvariantCulture);
						}
					}
					if (comparisonDifference.Action == (SynchronizationAction)0)
					{
						throw Logger.WriteException(new InvalidOperationException("No action tag found"));
					}
					if ((comparisonDifference.Local != null && string.IsNullOrEmpty(comparisonDifference.Local.FileName)) || (comparisonDifference.Remote != null && string.IsNullOrEmpty(comparisonDifference.Remote.FileName)))
					{
						throw Logger.WriteException(new InvalidOperationException("Missing file information"));
					}
					comparisonDifferenceCollection.InternalAdd(comparisonDifference);
				}
			}
			return comparisonDifferenceCollection;
		}
	}

	/// <summary>
	/// Executes a command in the session.
	/// </summary>
	/// <param name="command">The command to execute.</param>
	/// <returns>A <see cref="CommandExecutionResult"/> representing the result.</returns>
	public CommandExecutionResult ExecuteCommand(string command)
	{
		using (CreateCallstackAndLock())
		{
			CheckOpened();
			WriteCommand(string.Format(CultureInfo.InvariantCulture, "call {0}", new object[1] { command }));
			CommandExecutionResult commandExecutionResult = new CommandExecutionResult();
			using (RegisterOperationResult(commandExecutionResult))
			{
				using ElementLogReader elementLogReader = _reader.WaitForGroupAndCreateLogReader();
				using ElementLogReader elementLogReader2 = elementLogReader.WaitForNonEmptyElementAndCreateLogReader("call", LogReadFlags.ThrowFailures);
				while (elementLogReader2.Read((LogReadFlags)0))
				{
					if (elementLogReader2.GetEmptyElementValue("output", out var value))
					{
						commandExecutionResult.Output = value;
					}
					if (elementLogReader2.GetEmptyElementValue("erroroutput", out value))
					{
						commandExecutionResult.ErrorOutput = value;
					}
					if (elementLogReader2.GetEmptyElementValue("exitcode", out value))
					{
						commandExecutionResult.ExitCode = int.Parse(value, CultureInfo.InvariantCulture);
					}
				}
			}
			return commandExecutionResult;
		}
	}

	/// <summary>
	/// Gets file information for the specified path.
	/// </summary>
	/// <param name="path">The path of the file.</param>
	/// <returns>A <see cref="RemoteFileInfo"/> object containing file information.</returns>
	public RemoteFileInfo GetFileInfo(string path)
	{
		using (CreateCallstackAndLock())
		{
			CheckOpened();
			return DoGetFileInfo(path);
		}
	}

	/// <summary>
	/// Tries to get file information for the specified path.
	/// </summary>
	/// <param name="path">The path of the file.</param>
	/// <param name="fileInfo">The output file information.</param>
	/// <returns>True if file information was retrieved; otherwise, false.</returns>
	public bool TryGetFileInfo(string path, out RemoteFileInfo fileInfo)
	{
		using (CreateCallstackAndLock())
		{
			CheckOpened();
			try
			{
				_ignoreFailed = true;
				try
				{
					fileInfo = DoGetFileInfo(path);
				}
				finally
				{
					_ignoreFailed = false;
				}
				return true;
			}
			catch (SessionRemoteException)
			{
				fileInfo = null;
				return false;
			}
		}
	}

	/// <summary>
	/// Determines whether a file exists at the specified path.
	/// </summary>
	/// <param name="path">The path of the file.</param>
	/// <returns>True if the file exists; otherwise, false.</returns>
	public bool FileExists(string path)
	{
		RemoteFileInfo fileInfo;
		return TryGetFileInfo(path, out fileInfo);
	}

	/// <summary>
	/// Calculates the checksum of a file using the specified algorithm.
	/// </summary>
	/// <param name="algorithm">The checksum algorithm.</param>
	/// <param name="path">The path of the file.</param>
	/// <returns>The checksum as a byte array.</returns>
	public byte[] CalculateFileChecksum(string algorithm, string path)
	{
		using (CreateCallstackAndLock())
		{
			WriteCommand(string.Format(CultureInfo.InvariantCulture, "checksum -- \"{0}\" \"{1}\"", new object[2]
			{
				Tools.ArgumentEscape(algorithm),
				Tools.ArgumentEscape(path)
			}));
			string text = null;
			using (ElementLogReader elementLogReader = _reader.WaitForGroupAndCreateLogReader())
			{
				using ElementLogReader elementLogReader2 = elementLogReader.WaitForNonEmptyElementAndCreateLogReader("checksum", LogReadFlags.ThrowFailures);
				while (elementLogReader2.Read((LogReadFlags)0))
				{
					if (elementLogReader2.GetEmptyElementValue("checksum", out var value))
					{
						text = value;
					}
				}
				elementLogReader.ReadToEnd(LogReadFlags.ThrowFailures);
			}
			int length = text.Length;
			if (length % 2 != 0)
			{
				string message = string.Format(CultureInfo.CurrentCulture, "Invalid string representation of checksum - {0}", new object[1] { text });
				throw Logger.WriteException(new SessionLocalException(this, message));
			}
			int num = length / 2;
			byte[] array = new byte[num];
			for (int i = 0; i < num; i++)
			{
				array[i] = Convert.ToByte(text.Substring(i * 2, 2), 16);
			}
			return array;
		}
	}

	/// <summary>
	/// Creates a directory at the specified path.
	/// </summary>
	/// <param name="path">The path of the directory to create.</param>
	public void CreateDirectory(string path)
	{
		using (CreateCallstackAndLock())
		{
			CheckOpened();
			WriteCommand(string.Format(CultureInfo.InvariantCulture, "mkdir \"{0}\"", new object[1] { Tools.ArgumentEscape(path) }));
			using ElementLogReader elementLogReader = _reader.WaitForGroupAndCreateLogReader();
			using ElementLogReader reader = elementLogReader.WaitForNonEmptyElementAndCreateLogReader("mkdir", LogReadFlags.ThrowFailures);
			ReadElement(reader, (LogReadFlags)0);
			elementLogReader.ReadToEnd(LogReadFlags.ThrowFailures);
		}
	}

	/// <summary>
	/// Moves a file from the source path to the target path.
	/// </summary>
	/// <param name="sourcePath">The source file path.</param>
	/// <param name="targetPath">The target file path.</param>
	public void MoveFile(string sourcePath, string targetPath)
	{
		using (CreateCallstackAndLock())
		{
			CheckOpened();
			WriteCommand(string.Format(CultureInfo.InvariantCulture, "mv \"{0}\" \"{1}\"", new object[2]
			{
				Tools.ArgumentEscape(sourcePath),
				Tools.ArgumentEscape(targetPath)
			}));
			using ElementLogReader elementLogReader = _reader.WaitForGroupAndCreateLogReader();
			if (!elementLogReader.TryWaitForNonEmptyElement("mv", LogReadFlags.ThrowFailures))
			{
				throw Logger.WriteException(new SessionRemoteException(this, string.Format(CultureInfo.CurrentCulture, "{0} not found.", new object[1] { sourcePath })));
			}
			using ElementLogReader reader = elementLogReader.CreateLogReader();
			ReadElement(reader, (LogReadFlags)0);
			elementLogReader.ReadToEnd(LogReadFlags.ThrowFailures);
		}
	}

	/// <summary>
	/// Duplicates a file from the source path to the target path.
	/// </summary>
	/// <param name="sourcePath">The source file path.</param>
	/// <param name="targetPath">The target file path.</param>
	public void DuplicateFile(string sourcePath, string targetPath)
	{
		using (CreateCallstackAndLock())
		{
			CheckOpened();
			string text = Tools.ArgumentEscape(RemotePath.EscapeFileMask(sourcePath));
			string text2 = Tools.ArgumentEscape(targetPath);
			string command = string.Format(CultureInfo.InvariantCulture, "cp \"{0}\" \"{1}\"", new object[2] { text, text2 });
			WriteCommand(command);
			using ElementLogReader elementLogReader = _reader.WaitForGroupAndCreateLogReader();
			if (!elementLogReader.TryWaitForNonEmptyElement("cp", LogReadFlags.ThrowFailures))
			{
				throw Logger.WriteException(new SessionRemoteException(this, string.Format(CultureInfo.CurrentCulture, "{0} not found.", new object[1] { sourcePath })));
			}
			using ElementLogReader reader = elementLogReader.CreateLogReader();
			ReadElement(reader, (LogReadFlags)0);
			elementLogReader.ReadToEnd(LogReadFlags.ThrowFailures);
		}
	}

	/// <summary>
	/// Escapes a file mask for remote operations. (Obsolete)
	/// </summary>
	/// <param name="fileMask">The file mask to escape.</param>
	/// <returns>The escaped file mask.</returns>
	[Obsolete("Use RemotePath.EscapeFileMask")]
	public string EscapeFileMask(string fileMask)
	{
		return RemotePath.EscapeFileMask(fileMask);
	}

	/// <summary>
	/// Translates a remote path to a local path. (Obsolete)
	/// </summary>
	/// <param name="remotePath">The remote path.</param>
	/// <param name="remoteRoot">The remote root.</param>
	/// <param name="localRoot">The local root.</param>
	/// <returns>The translated local path.</returns>
	[Obsolete("Use RemotePath.TranslateRemotePathToLocal")]
	public string TranslateRemotePathToLocal(string remotePath, string remoteRoot, string localRoot)
	{
		return RemotePath.TranslateRemotePathToLocal(remotePath, remoteRoot, localRoot);
	}

	/// <summary>
	/// Translates a local path to a remote path. (Obsolete)
	/// </summary>
	/// <param name="localPath">The local path.</param>
	/// <param name="localRoot">The local root.</param>
	/// <param name="remoteRoot">The remote root.</param>
	/// <returns>The translated remote path.</returns>
	[Obsolete("Use RemotePath.TranslateLocalPathToRemote")]
	public string TranslateLocalPathToRemote(string localPath, string localRoot, string remoteRoot)
	{
		return RemotePath.TranslateLocalPathToRemote(localPath, localRoot, remoteRoot);
	}

	/// <summary>
	/// Combines two paths. (Obsolete)
	/// </summary>
	/// <param name="path1">The first path.</param>
	/// <param name="path2">The second path.</param>
	/// <returns>The combined path.</returns>
	[Obsolete("Use RemotePath.CombinePaths")]
	public string CombinePaths(string path1, string path2)
	{
		return RemotePath.Combine(path1, path2);
	}

	/// <summary>
	/// Adds a raw configuration setting.
	/// </summary>
	/// <param name="setting">The setting name.</param>
	/// <param name="value">The setting value.</param>
	public void AddRawConfiguration(string setting, string value)
	{
		CheckNotOpened();
		RawConfiguration.Add(setting, value);
	}

    /*
	 * Original code
	 *
	 * [ComRegisterFunction]
	 * private static void ComRegister(Type t)
	 * {
	 * 	Assembly assembly = Assembly.GetAssembly(t);
	 * 	string value = Marshal.GetTypeLibGuidForAssembly(assembly).ToString();
	 * 	Marshal.GetTypeLibVersionForAssembly(assembly, out var majorVersion, out var minorVersion);
	 * 	string value2 = $"{majorVersion}.{minorVersion}";
	 * 	RegistryKey classesRoot = Registry.ClassesRoot;
	 * 	classesRoot.CreateSubKey(GetTypeLibKey(t)).SetValue(null, value);
	 * 	classesRoot.CreateSubKey(GetVersionKey(t)).SetValue(null, value2);
	 * }
	 */

    /// <summary>
    /// Called by the COM infrastructure when this type is registered. Writes the assembly GUID and
    /// version into the registry under the type's CLSID key.
    /// </summary>
    /// <param name="t">The type being registered.</param>
    /// <exception cref="InvalidOperationException">Thrown when the assembly does not carry a <see cref="GuidAttribute"/>.</exception>
    [ComRegisterFunction]
    private static void ComRegister(Type t)
    {
        Assembly assembly = Assembly.GetAssembly(t);
        string value = assembly.GetCustomAttribute<GuidAttribute>()?.Value ?? throw new InvalidOperationException("Assembly GUID not found");
        AssemblyName name = assembly.GetName();
        string value2 = $"{name.Version.Major}.{name.Version.Minor}";
        RegistryKey classesRoot = Registry.ClassesRoot;
        classesRoot.CreateSubKey(GetTypeLibKey(t)).SetValue(null, value);
        classesRoot.CreateSubKey(GetVersionKey(t)).SetValue(null, value2);
    }
    
	/// <summary>
	/// Called by the COM infrastructure when this type is unregistered. Removes the TypeLib and Version
	/// subkeys from the registry under the type's CLSID key.
	/// </summary>
	/// <param name="t">The type being unregistered.</param>
	[ComUnregisterFunction]
	private static void ComUnregister(Type t)
	{
		RegistryKey classesRoot = Registry.ClassesRoot;
		classesRoot.DeleteSubKey(GetTypeLibKey(t), throwOnMissingSubKey: false);
		classesRoot.DeleteSubKey(GetVersionKey(t), throwOnMissingSubKey: false);
	}

	/// <summary>
	/// Returns the registry path for the CLSID of the specified type (e.g., <c>"CLSID\{GUID}"</c>).
	/// </summary>
	/// <param name="t">The type whose CLSID key is required.</param>
	/// <returns>A registry key path string for the type's CLSID.</returns>
	private static string GetClsidKey(Type t)
	{
		return "CLSID\\" + t.GUID.ToString("B").ToUpperInvariant();
	}

	/// <summary>
	/// Returns the registry path for the TypeLib subkey under the CLSID of the specified type.
	/// </summary>
	/// <param name="t">The type whose TypeLib key is required.</param>
	/// <returns>A registry key path string (e.g., <c>"CLSID\{GUID}\TypeLib"</c>).</returns>
	private static string GetTypeLibKey(Type t)
	{
		return GetClsidKey(t) + "\\TypeLib";
	}

	/// <summary>
	/// Returns the registry path for the Version subkey under the CLSID of the specified type.
	/// </summary>
	/// <param name="t">The type whose Version key is required.</param>
	/// <returns>A registry key path string (e.g., <c>"CLSID\{GUID}\Version"</c>).</returns>
	private static string GetVersionKey(Type t)
	{
		return GetClsidKey(t) + "\\Version";
	}

	/// <summary>
	/// Populates file metadata fields on <paramref name="fileInfo"/> from the current XML log element
	/// read by <paramref name="fileReader"/>.
	/// </summary>
	/// <param name="fileInfo">The <see cref="RemoteFileInfo"/> object to populate.</param>
	/// <param name="fileReader">The log reader positioned at a file-attribute element.</param>
	private void ReadFile(RemoteFileInfo fileInfo, CustomLogReader fileReader)
	{
		using (Logger.CreateCallstack())
		{
			if (fileReader.GetEmptyElementValue("type", out var value))
			{
				fileInfo.FileType = value[0];
			}
			else if (fileReader.GetEmptyElementValue("size", out value))
			{
				fileInfo.Length = long.Parse(value, CultureInfo.InvariantCulture);
			}
			else if (fileReader.GetEmptyElementValue("modification", out value))
			{
				fileInfo.LastWriteTime = XmlConvert.ToDateTime(value, XmlDateTimeSerializationMode.Local);
			}
			else if (fileReader.GetEmptyElementValue("permissions", out value))
			{
				fileInfo.FilePermissions = FilePermissions.CreateReadOnlyFromText(value);
			}
			else if (fileReader.GetEmptyElementValue("owner", out value))
			{
				fileInfo.Owner = value;
			}
			else if (fileReader.GetEmptyElementValue("group", out value))
			{
				fileInfo.Group = value;
			}
		}
	}

	/// <summary>
	/// Returns a command-line switch string (<c>"-name"</c>) when <paramref name="flag"/> is <c>true</c>,
	/// or <c>null</c> when it is <c>false</c>.
	/// </summary>
	/// <param name="flag">The boolean value that controls whether the switch is emitted.</param>
	/// <param name="name">The name of the switch (without the leading hyphen).</param>
	/// <returns>The formatted switch string, or <c>null</c>.</returns>
	internal static string BooleanSwitch(bool flag, string name)
	{
		if (!flag)
		{
			return null;
		}
		return string.Format(CultureInfo.InvariantCulture, "-{0}", new object[1] { name });
	}

	/// <summary>
	/// Returns either <c>"-onName"</c> or <c>"-offName"</c> depending on the value of <paramref name="flag"/>.
	/// </summary>
	/// <param name="flag">When <c>true</c> the on-switch is returned; otherwise the off-switch.</param>
	/// <param name="onName">The switch name emitted when <paramref name="flag"/> is <c>true</c>.</param>
	/// <param name="offName">The switch name emitted when <paramref name="flag"/> is <c>false</c>.</param>
	/// <returns>The formatted switch string.</returns>
	internal static string BooleanSwitch(bool flag, string onName, string offName)
	{
		if (!flag)
		{
			return string.Format(CultureInfo.InvariantCulture, "-{0}", new object[1] { offName });
		}
		return string.Format(CultureInfo.InvariantCulture, "-{0}", new object[1] { onName });
	}

	/// <summary>
	/// Adds a completed synchronization transfer to the result as an upload or download depending on
	/// its side, and raises the <see cref="FileTransferred"/> event.
	/// </summary>
	/// <param name="result">The synchronization result to update.</param>
	/// <param name="transfer">The transfer event arguments; no action is taken when <c>null</c>.</param>
	private void AddSynchronizationTransfer(SynchronizationResult result, TransferEventArgs transfer)
	{
		if (transfer != null)
		{
			if (transfer.Side == ProgressSide.Local)
			{
				result.AddUpload(transfer);
			}
			else
			{
				result.AddDownload(transfer);
			}
			RaiseFileTransferredEvent(transfer);
		}
	}

	/// <summary>
	/// Ensures that the path ends with a forward slash, appending one if necessary.
	/// </summary>
	/// <param name="path">The path to normalize.</param>
	/// <returns>The path with a trailing slash, or the original value if it is null or empty.</returns>
	private static string IncludeTrailingSlash(string path)
	{
		if (!string.IsNullOrEmpty(path) && !path.EndsWith("/", StringComparison.Ordinal))
		{
			path += "/";
		}
		return path;
	}

	/// <summary>
	/// Terminates the WinSCP child process, disposes all log readers, and deletes the temporary XML log
	/// file unless <see cref="XmlLogPreserve"/> is set.
	/// </summary>
	private void Cleanup()
	{
		using (Logger.CreateCallstack())
		{
			if (_process != null)
			{
				Logger.WriteLine("Terminating process");
				try
				{
					try
					{
						WriteCommand("exit");
						_process.Close();
					}
					finally
					{
						_process.Dispose();
						_process = null;
					}
				}
				catch (Exception ex)
				{
					Logger.WriteLine("Process cleanup Exception: {0}", ex);
				}
			}
			Logger.WriteLine("Disposing log readers");
			if (_reader != null)
			{
				_reader.Dispose();
				_reader = null;
			}
			if (_logReader != null)
			{
				_logReader.Dispose();
				_logReader = null;
			}
			if (XmlLogPath != null && File.Exists(XmlLogPath) && !XmlLogPreserve)
			{
				Logger.WriteLine("Deleting XML log file [{0}]", XmlLogPath);
				try
				{
					File.Delete(XmlLogPath);
				}
				catch (DirectoryNotFoundException ex2)
				{
					Logger.WriteLine("XML log cleanup DirectoryNotFoundException: {0}", ex2);
				}
				catch (IOException ex3)
				{
					Logger.WriteLine("XML log cleanup IOException: {0}", ex3);
				}
				catch (UnauthorizedAccessException ex4)
				{
					Logger.WriteLine("XML log cleanup UnauthorizedAccessException: {0}", ex4);
				}
				_xmlLogPath = null;
			}
		}
	}

	/// <summary>
	/// Sends a command to the WinSCP process, using the same string for both the command and its log entry.
	/// </summary>
	/// <param name="command">The command string to send and log.</param>
	private void WriteCommand(string command)
	{
		WriteCommand(command, command);
	}

	/// <summary>
	/// Sends a command to the WinSCP process with a separate sanitized string for log output.
	/// </summary>
	/// <param name="command">The actual command string sent to the process.</param>
	/// <param name="log">The version of the command written to the log (may have passwords masked).</param>
	private void WriteCommand(string command, string log)
	{
		Logger.WriteLine("Command: [{0}]", log);
		_process.ExecuteCommand(command, log);
		GotOutput();
	}

	/// <summary>
	/// Reads all remaining elements from the log reader, discarding content, until EOF is reached.
	/// </summary>
	/// <param name="reader">The log reader to drain.</param>
	/// <param name="flags">Flags controlling error handling during reading.</param>
	private static void ReadElement(CustomLogReader reader, LogReadFlags flags)
	{
		while (reader.Read(flags))
		{
		}
	}

	/// <summary>
	/// Converts a <see cref="SessionOptions"/> instance into a WinSCP URL and command-line switch string,
	/// producing separate versions for the actual command and the log (with credentials masked).
	/// </summary>
	/// <param name="sessionOptions">The session options to convert.</param>
	/// <param name="scanFingerprint">
	/// <c>true</c> when building the arguments for a fingerprint scan; credentials and certain options
	/// are omitted in this mode.
	/// </param>
	/// <param name="command">Output: the full command string including URL and switches.</param>
	/// <param name="log">Output: a log-safe version of the command with sensitive values replaced by <c>***</c>.</param>
	/// <exception cref="ArgumentException">Thrown when the session options contain invalid or inconsistent values.</exception>
	private void SessionOptionsToUrlAndSwitches(SessionOptions sessionOptions, bool scanFingerprint, out string command, out string log)
	{
		using (Logger.CreateCallstack())
		{
			if (sessionOptions.Secure && sessionOptions.Protocol != Protocol.Webdav && sessionOptions.Protocol != Protocol.S3)
			{
				throw Logger.WriteException(new ArgumentException("SessionOptions.Secure is set, but SessionOptions.Protocol is not Protocol.Webdav nor Protocol.S3."));
			}
			string text = sessionOptions.Protocol switch
			{
				Protocol.Sftp => "sftp://", 
				Protocol.Scp => "scp://", 
				Protocol.Ftp => "ftp://", 
				Protocol.Webdav => sessionOptions.Secure ? "davs://" : "dav://", 
				Protocol.S3 => sessionOptions.Secure ? "s3://" : "s3plain://", 
				_ => throw Logger.WriteException(new ArgumentException(string.Format(CultureInfo.CurrentCulture, "{0} is not supported", new object[1] { sessionOptions.Protocol }))), 
			};
			bool flag;
			if (!scanFingerprint)
			{
				flag = !string.IsNullOrEmpty(sessionOptions.UserName);
				if (flag)
				{
					text += UriEscape(sessionOptions.UserName);
				}
			}
			else
			{
				flag = false;
			}
			string text2 = text;
			string text3 = text;
			if (sessionOptions.SecurePassword != null && !scanFingerprint)
			{
				if (!flag)
				{
					throw Logger.WriteException(new ArgumentException("SessionOptions.Password is set, but SessionOptions.UserName is not."));
				}
				text2 = text2 + ":" + UriEscape(sessionOptions.Password);
				text3 += ":***";
			}
			string text4 = string.Empty;
			if (flag)
			{
				text4 += "@";
			}
			if (string.IsNullOrEmpty(sessionOptions.HostName))
			{
				throw Logger.WriteException(new ArgumentException("SessionOptions.HostName is not set."));
			}
			text4 += UriEscape(sessionOptions.HostName);
			if (sessionOptions.PortNumber != 0)
			{
				text4 = text4 + ":" + sessionOptions.PortNumber.ToString(CultureInfo.InvariantCulture);
			}
			if (!string.IsNullOrEmpty(sessionOptions.RootPath) && !scanFingerprint)
			{
				if (sessionOptions.Protocol != Protocol.Webdav && sessionOptions.Protocol != Protocol.S3)
				{
					throw Logger.WriteException(new ArgumentException("SessionOptions.RootPath is set, but SessionOptions.Protocol is not Protocol.Webdav nor Protocol.S3."));
				}
				text4 += sessionOptions.RootPath;
			}
			text2 += text4;
			text3 += text4;
			SessionOptionsToSwitches(sessionOptions, scanFingerprint, out var arguments, out var logArguments);
			Tools.AddRawParameters(ref arguments, sessionOptions.RawSettings, "-rawsettings", count: false);
			Tools.AddRawParameters(ref logArguments, sessionOptions.RawSettings, "-rawsettings", count: false);
			if (!string.IsNullOrEmpty(arguments))
			{
				arguments = " " + arguments;
				logArguments = " " + logArguments;
			}
			command = "\"" + Tools.ArgumentEscape(text2) + "\"" + arguments;
			log = "\"" + Tools.ArgumentEscape(text3) + "\"" + logArguments;
		}
	}

	/// <summary>
	/// Converts the non-URL portion of a <see cref="SessionOptions"/> instance into WinSCP command-line switches,
	/// producing a separate log-safe version with passwords masked.
	/// </summary>
	/// <param name="sessionOptions">The session options to convert.</param>
	/// <param name="scanFingerprint">
	/// <c>true</c> when building arguments for a fingerprint scan; credential-related switches are omitted.
	/// </param>
	/// <param name="arguments">Output: the full switch string.</param>
	/// <param name="logArguments">Output: a log-safe version of the switches with sensitive values masked.</param>
	/// <exception cref="ArgumentException">Thrown when the session options contain invalid or inconsistent values.</exception>
	private void SessionOptionsToSwitches(SessionOptions sessionOptions, bool scanFingerprint, out string arguments, out string logArguments)
	{
		using (Logger.CreateCallstack())
		{
			List<string> list = new List<string>();
			if (!string.IsNullOrEmpty(sessionOptions.SshHostKeyFingerprint) || (sessionOptions.SshHostKeyPolicy != SshHostKeyPolicy.Check && !scanFingerprint))
			{
				if (!sessionOptions.IsSsh)
				{
					throw Logger.WriteException(new ArgumentException("SessionOptions.SshHostKeyFingerprint is set or sessionOptions.SshHostKeyPolicy has not the default value Check, but SessionOptions.Protocol is neither Protocol.Sftp nor Protocol.Scp."));
				}
				string text = sessionOptions.SshHostKeyFingerprint;
				switch (sessionOptions.SshHostKeyPolicy)
				{
				case SshHostKeyPolicy.GiveUpSecurityAndAcceptAny:
					text = AddStarToList(text);
					Logger.WriteLine("WARNING! Giving up security and accepting any key as configured");
					break;
				case SshHostKeyPolicy.AcceptNew:
					if (!string.IsNullOrEmpty(text))
					{
						throw Logger.WriteException(new ArgumentException("SessionOptions.SshHostKeyFingerprint is set and SshHostKeyPolicy is not Check"));
					}
					text = "acceptnew";
					break;
				}
				list.Add(FormatSwitch("hostkey", text));
			}
			else if (sessionOptions.IsSsh && DefaultConfigurationInternal && !scanFingerprint)
			{
				throw Logger.WriteException(new ArgumentException("SessionOptions.Protocol is Protocol.Sftp or Protocol.Scp, but SessionOptions.SshHostKeyFingerprint is not set."));
			}
			bool flag = !string.IsNullOrEmpty(sessionOptions.SshPrivateKeyPath);
			bool flag2 = !string.IsNullOrEmpty(sessionOptions.SshPrivateKey);
			if ((flag || flag2) && !scanFingerprint)
			{
				if (!sessionOptions.IsSsh)
				{
					throw Logger.WriteException(new ArgumentException("SessionOptions.SshPrivateKeyPath or SessionOptions.SshPrivateKey is set, but SessionOptions.Protocol is neither Protocol.Sftp nor Protocol.Scp."));
				}
				if (flag && flag2)
				{
					throw Logger.WriteException(new ArgumentException("Set only one of SessionOptions.SshPrivateKeyPath or SessionOptions.SshPrivateKey."));
				}
				string value = ((!flag) ? ("@" + GenerateInMemoryPrivateKey(sessionOptions)) : sessionOptions.SshPrivateKeyPath);
				list.Add(FormatSwitch("privatekey", value));
			}
			if (!string.IsNullOrEmpty(sessionOptions.TlsClientCertificatePath) && !scanFingerprint)
			{
				if (!sessionOptions.IsTls)
				{
					throw Logger.WriteException(new ArgumentException("SessionOptions.TlsClientCertificatePath is set, but neither SessionOptions.FtpSecure nor SessionOptions.Secure is enabled."));
				}
				list.Add(FormatSwitch("clientcert", sessionOptions.TlsClientCertificatePath));
			}
			if (sessionOptions.FtpSecure != FtpSecure.None)
			{
				if (sessionOptions.Protocol != Protocol.Ftp)
				{
					throw Logger.WriteException(new ArgumentException("SessionOptions.FtpSecure is not FtpSecure.None, but SessionOptions.Protocol is not Protocol.Ftp."));
				}
				switch (sessionOptions.FtpSecure)
				{
				case FtpSecure.Implicit:
					list.Add(FormatSwitch("implicit"));
					break;
				case FtpSecure.Explicit:
					list.Add(FormatSwitch("explicit"));
					break;
				default:
					throw Logger.WriteException(new ArgumentException(string.Format(CultureInfo.CurrentCulture, "{0} is not supported", new object[1] { sessionOptions.FtpSecure })));
				}
			}
			if ((!string.IsNullOrEmpty(sessionOptions.TlsHostCertificateFingerprint) || sessionOptions.GiveUpSecurityAndAcceptAnyTlsHostCertificate) && !scanFingerprint)
			{
				if (!sessionOptions.IsTls)
				{
					throw Logger.WriteException(new ArgumentException("SessionOptions.TlsHostCertificateFingerprint or SessionOptions.GiveUpSecurityAndAcceptAnyTlsHostCertificate is set, but neither SessionOptions.FtpSecure nor SessionOptions.Secure is enabled."));
				}
				string text2 = sessionOptions.TlsHostCertificateFingerprint;
				if (sessionOptions.GiveUpSecurityAndAcceptAnyTlsHostCertificate)
				{
					Logger.WriteLine("WARNING! Giving up security and accepting any certificate as configured");
					text2 = AddStarToList(text2);
				}
				list.Add(FormatSwitch("certificate", text2));
			}
			if (sessionOptions.Protocol == Protocol.Ftp && !scanFingerprint)
			{
				list.Add(FormatSwitch("passive", sessionOptions.FtpMode == FtpMode.Passive));
			}
			list.Add(FormatSwitch("timeout", (int)sessionOptions.Timeout.TotalSeconds));
			List<string> list2 = new List<string>(list);
			if (sessionOptions.SecurePrivateKeyPassphrase != null && !scanFingerprint)
			{
				if (string.IsNullOrEmpty(sessionOptions.SshPrivateKeyPath) && string.IsNullOrEmpty(sessionOptions.SshPrivateKey) && string.IsNullOrEmpty(sessionOptions.TlsClientCertificatePath))
				{
					throw Logger.WriteException(new ArgumentException("SessionOptions.PrivateKeyPassphrase is set, but neither SessionOptions.SshPrivateKeyPath, SessionOptions.SshPrivateKey nor SessionOptions.TlsClientCertificatePath is set."));
				}
				list.Add(FormatSwitch("passphrase", sessionOptions.PrivateKeyPassphrase));
				list2.Add(FormatSwitch("passphrase", "***"));
			}
			if (sessionOptions.SecureNewPassword != null && !scanFingerprint)
			{
				if (sessionOptions.SecurePassword == null)
				{
					throw Logger.WriteException(new ArgumentException("SessionOptions.SecureNewPassword is set, but SessionOptions.SecurePassword is not."));
				}
				list.Add(FormatSwitch("newpassword", sessionOptions.NewPassword));
				list2.Add(FormatSwitch("newpassword", "***"));
			}
			arguments = string.Join(" ", list.ToArray());
			logArguments = string.Join(" ", list2.ToArray());
		}
	}

	/// <summary>
	/// Converts an in-memory SSH private key string into the hex-encoded format required by WinSCP.
	/// </summary>
	/// <param name="sessionOptions">The session options containing the <see cref="SessionOptions.SshPrivateKey"/> value.</param>
	/// <returns>The private key encoded as an uppercase hex string without separators.</returns>
	internal static string GenerateInMemoryPrivateKey(SessionOptions sessionOptions)
	{
		return BitConverter.ToString(Encoding.Default.GetBytes(sessionOptions.SshPrivateKey)).Replace("-", "");
	}

	/// <summary>
	/// Appends a wildcard (<c>*</c>) entry to a semicolon-separated list, creating the list if it is empty.
	/// </summary>
	/// <param name="list">The existing semicolon-separated list, or <c>null</c>/empty to start a new one.</param>
	/// <returns>The list with <c>*</c> appended.</returns>
	private static string AddStarToList(string list)
	{
		return (string.IsNullOrEmpty(list) ? string.Empty : (list + ";")) + "*";
	}

	/// <summary>
	/// Sends the WinSCP <c>stat</c> command and parses the resulting XML log entry into a
	/// <see cref="RemoteFileInfo"/> object.
	/// </summary>
	/// <param name="path">The remote file path to query.</param>
	/// <returns>A populated <see cref="RemoteFileInfo"/> for the specified path.</returns>
	private RemoteFileInfo DoGetFileInfo(string path)
	{
		using (Logger.CreateCallstack())
		{
			WriteCommand(string.Format(CultureInfo.InvariantCulture, "stat -- \"{0}\"", new object[1] { Tools.ArgumentEscape(path) }));
			RemoteFileInfo remoteFileInfo = new RemoteFileInfo();
			using (ElementLogReader elementLogReader = _reader.WaitForGroupAndCreateLogReader())
			{
				using ElementLogReader elementLogReader2 = elementLogReader.WaitForNonEmptyElementAndCreateLogReader("stat", LogReadFlags.ThrowFailures);
				while (elementLogReader2.Read((LogReadFlags)0))
				{
					if (elementLogReader2.GetEmptyElementValue("filename", out var value))
					{
						string text = value;
						int num = text.LastIndexOf('/');
						if (num >= 0)
						{
							text = text.Substring(num + 1);
						}
						remoteFileInfo.Name = text;
						remoteFileInfo.FullName = value;
					}
					else
					{
						if (!elementLogReader2.IsNonEmptyElement("file"))
						{
							continue;
						}
						using ElementLogReader elementLogReader3 = elementLogReader2.CreateLogReader();
						while (elementLogReader3.Read((LogReadFlags)0))
						{
							ReadFile(remoteFileInfo, elementLogReader3);
						}
					}
				}
				elementLogReader.ReadToEnd(LogReadFlags.ThrowFailures);
			}
			return remoteFileInfo;
		}
	}

	/// <summary>
	/// Formats a boolean WinSCP command-line switch as <c>"-key"</c>.
	/// </summary>
	/// <param name="key">The switch name (without the leading hyphen).</param>
	/// <returns>The formatted switch string.</returns>
	internal static string FormatSwitch(string key)
	{
		return string.Format(CultureInfo.InvariantCulture, "-{0}", new object[1] { key });
	}

	/// <summary>
	/// Formats a string-valued WinSCP command-line switch as <c>-key="value"</c>, escaping the value.
	/// </summary>
	/// <param name="key">The switch name (without the leading hyphen).</param>
	/// <param name="value">The string value to assign to the switch.</param>
	/// <returns>The formatted switch string.</returns>
	internal static string FormatSwitch(string key, string value)
	{
		return string.Format(CultureInfo.InvariantCulture, "-{0}=\"{1}\"", new object[2]
		{
			key,
			Tools.ArgumentEscape(value)
		});
	}

	/// <summary>
	/// Formats an integer-valued WinSCP command-line switch as <c>-key=value</c>.
	/// </summary>
	/// <param name="key">The switch name (without the leading hyphen).</param>
	/// <param name="value">The integer value to assign to the switch.</param>
	/// <returns>The formatted switch string.</returns>
	internal static string FormatSwitch(string key, int value)
	{
		return string.Format(CultureInfo.InvariantCulture, "-{0}={1}", new object[2]
		{
			key,
			value.ToString(CultureInfo.InvariantCulture)
		});
	}

	/// <summary>
	/// Formats a boolean WinSCP command-line switch as <c>-key=1</c> or <c>-key=0</c>.
	/// </summary>
	/// <param name="key">The switch name (without the leading hyphen).</param>
	/// <param name="value"><c>true</c> produces <c>1</c>; <c>false</c> produces <c>0</c>.</param>
	/// <returns>The formatted switch string.</returns>
	internal static string FormatSwitch(string key, bool value)
	{
		return FormatSwitch(key, value ? 1 : 0);
	}

	/// <summary>
	/// Percent-encodes a string for safe embedding in a URI.
	/// </summary>
	/// <param name="s">The string to encode.</param>
	/// <returns>The URI-encoded string.</returns>
	private static string UriEscape(string s)
	{
		return Uri.EscapeDataString(s);
	}

	/// <summary>
	/// Records the current time as the timestamp of the most recent output received from WinSCP,
	/// resetting the inactivity timeout counter.
	/// </summary>
	internal void GotOutput()
	{
		_lastOutput = DateTime.Now;
	}

	/// <summary>
	/// Handles output data received from the WinSCP process, adding it to the output/error buffers and
	/// scheduling the <see cref="OutputDataReceived"/> event for dispatch.
	/// </summary>
	/// <param name="sender">The source of the event.</param>
	/// <param name="e">The event arguments containing the output data; <c>null</c> indicates incomplete progress output.</param>
	private void ProcessOutputDataReceived(object sender, OutputDataReceivedEventArgs e)
	{
		if (e == null)
		{
			Logger.WriteLine("Got incomplete progress output");
		}
		else
		{
			Logger.WriteLine("Scheduling output: [{0}]", e.Data);
			string item = e.Data.TrimEnd('\r');
			lock (Output)
			{
				Output.InternalAdd(item);
				if (Output.Count > 1000)
				{
					Output.InternalRemoveFirst();
				}
				if (e.Error)
				{
					_error.InternalAdd(item);
					if (_error.Count > 1000)
					{
						_error.InternalRemoveFirst();
					}
				}
			}
			ScheduleEvent(delegate
			{
				RaiseOutputDataReceived(e.Data, e.Error);
			});
		}
		GotOutput();
	}

	/// <summary>
	/// Enqueues an action for dispatch on the session's event thread and signals the event wait handle.
	/// </summary>
	/// <param name="action">The action to enqueue.</param>
	private void ScheduleEvent(Action action)
	{
		lock (_events)
		{
			_events.Add(action);
			_eventsEvent.Set();
		}
	}

	/// <summary>
	/// Checks whether the session has timed out or been aborted, and throws the appropriate exception
	/// when either condition is detected.
	/// </summary>
	/// <param name="additional">Optional context message appended to the timeout exception message.</param>
	/// <exception cref="TimeoutException">Thrown when no output has been received within the configured timeout.</exception>
	/// <exception cref="SessionLocalException">Thrown when the session was aborted via <see cref="Abort"/>.</exception>
	internal void CheckForTimeout(string additional = null)
	{
		TimeSpan timeSpan = Timeout;
		if (timeSpan < _sessionTimeout)
		{
			timeSpan = _sessionTimeout + TimeSpan.FromSeconds(1.0);
		}
		if (DateTime.Now - _lastOutput > timeSpan)
		{
			Logger.WriteLine("Timeout waiting for WinSCP to respond - asking for callstack");
			_process.RequestCallstack();
			string text = "Timeout waiting for WinSCP to respond";
			if (additional != null)
			{
				text = text + " - " + additional;
			}
			_logReader?.SetTimeouted();
			Cleanup();
			throw Logger.WriteException(new TimeoutException(text));
		}
		if (_aborted)
		{
			throw Logger.WriteException(new SessionLocalException(this, "Aborted."));
		}
		if (_throwStdOut && _process.StdOut != null && _process.StdOut.ReadAvailable(1))
		{
			Logger.WriteLine("Got data");
			throw new StdOutException();
		}
	}

	/// <summary>
	/// Raises the <see cref="FileTransferred"/> event with the given transfer arguments.
	/// </summary>
	/// <param name="args">The event arguments describing the completed transfer.</param>
	private void RaiseFileTransferredEvent(TransferEventArgs args)
	{
		Logger.WriteLine("FileTransferredEvent: [{0}]", args.FileName);
		this.FileTransferred?.Invoke(this, args);
	}

	/// <summary>
	/// Raises the <see cref="Failed"/> event (when not suppressed) and records the failure on all
	/// active operation results.
	/// </summary>
	/// <param name="e">The remote exception describing the failure.</param>
	internal void RaiseFailed(SessionRemoteException e)
	{
		Logger.WriteLine("Failed: [{0}]", e);
		if (this.Failed != null && !_ignoreFailed)
		{
			this.Failed(this, new FailedEventArgs
			{
				Error = e
			});
		}
		foreach (OperationResultBase operationResult in _operationResults)
		{
			operationResult.AddFailure(e);
		}
	}

	/// <summary>
	/// Throws an <see cref="InvalidOperationException"/> if the session has been disposed or aborted.
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown when the session is disposed or was aborted.</exception>
	private void CheckNotDisposed()
	{
		if (_disposed)
		{
			throw Logger.WriteException(new InvalidOperationException("Object is disposed"));
		}
		if (_aborted)
		{
			throw Logger.WriteException(new InvalidOperationException("Session was aborted"));
		}
	}

	/// <summary>
	/// Throws an <see cref="InvalidOperationException"/> if the session is not currently open.
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown when the session has not been opened.</exception>
	private void CheckOpened()
	{
		if (!Opened)
		{
			throw Logger.WriteException(new InvalidOperationException("Session is not opened"));
		}
	}

	/// <summary>
	/// Throws an <see cref="InvalidOperationException"/> if the session is already open.
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown when the session is already opened.</exception>
	private void CheckNotOpened()
	{
		if (Opened)
		{
			throw Logger.WriteException(new InvalidOperationException("Session is already opened"));
		}
	}

	/// <summary>
	/// Raises the <see cref="OutputDataReceived"/> event with the specified output data.
	/// </summary>
	/// <param name="data">The output text received from the WinSCP process.</param>
	/// <param name="error"><c>true</c> if the data was received on the error stream; otherwise, <c>false</c>.</param>
	private void RaiseOutputDataReceived(string data, bool error)
	{
		Logger.WriteLine("Output: [{0}]", data);
		this.OutputDataReceived?.Invoke(this, new OutputDataReceivedEventArgs(data, error));
	}

	/// <summary>
	/// Dispatches all queued session events within the specified time interval.
	/// </summary>
	/// <param name="interval">The maximum number of milliseconds to spend dispatching events.</param>
	internal void DispatchEvents(int interval)
	{
		DateTime now = DateTime.Now;
		while (_eventsEvent.WaitOne(interval, exitContext: false))
		{
			lock (_events)
			{
				foreach (Action @event in _events)
				{
					@event();
				}
				_events.Clear();
			}
			interval -= (int)(DateTime.Now - now).TotalMilliseconds;
			if (interval >= 0)
			{
				now = DateTime.Now;
				continue;
			}
			break;
		}
	}

	/// <summary>
	/// Adds an operation result to the active-results list and returns a guard that removes it on disposal.
	/// </summary>
	/// <param name="operationResult">The operation result to register.</param>
	/// <returns>An <see cref="IDisposable"/> guard that unregisters the result when disposed.</returns>
	private IDisposable RegisterOperationResult(OperationResultBase operationResult)
	{
		_operationResults.Add(operationResult);
		return new OperationResultGuard(this, operationResult);
	}

	/// <summary>
	/// Removes the specified operation result from the active-results list.
	/// </summary>
	/// <param name="operationResult">The operation result to remove.</param>
	internal void UnregisterOperationResult(OperationResultBase operationResult)
	{
		_operationResults.Remove(operationResult);
	}

	/// <summary>
	/// Increments the progress-handling reference count and returns a guard object that decrements
	/// it (and dispatches any pending events) on disposal.
	/// </summary>
	/// <returns>An <see cref="IDisposable"/> that disables progress handling when disposed.</returns>
	private IDisposable CreateProgressHandler()
	{
		using (Logger.CreateCallstack())
		{
			_progressHandling++;
			return new ProgressHandler(this);
		}
	}

	/// <summary>
	/// Dispatches any pending events and decrements the progress-handling reference count.
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown when progress handling is not currently enabled.</exception>
	internal void DisableProgressHandling()
	{
		using (Logger.CreateCallstack())
		{
			if (_progressHandling <= 0)
			{
				throw Logger.WriteException(new InvalidOperationException("Progress handling not enabled"));
			}
			DispatchEvents(0);
			_progressHandling--;
		}
	}

	/// <summary>
	/// Schedules a query-received event for dispatch and blocks until the subscriber has provided a choice.
	/// </summary>
	/// <param name="args">The query event arguments to pass to the <see cref="QueryReceived"/> handler.</param>
	internal void ProcessChoice(QueryReceivedEventArgs args)
	{
		if (_queryReceived != null)
		{
			_choiceEvent.Reset();
			ScheduleEvent(delegate
			{
				Choice(args);
			});
			_choiceEvent.WaitOne();
		}
	}

	/// <summary>
	/// Invokes the <see cref="QueryReceived"/> handler synchronously and then signals the choice event
	/// so that <see cref="ProcessChoice"/> can unblock.
	/// </summary>
	/// <param name="args">The query event arguments.</param>
	private void Choice(QueryReceivedEventArgs args)
	{
		_queryReceived?.Invoke(this, args);
		_choiceEvent.Set();
	}

	/// <summary>
	/// Schedules a file transfer progress event for dispatch on the event thread.
	/// </summary>
	/// <param name="args">The progress event arguments to dispatch.</param>
	internal void ProcessProgress(FileTransferProgressEventArgs args)
	{
		ScheduleEvent(delegate
		{
			Progress(args);
		});
	}

	/// <summary>
	/// Invokes the <see cref="FileTransferProgress"/> handler and, if the subscriber requests
	/// cancellation, instructs the WinSCP process to cancel the current transfer.
	/// </summary>
	/// <param name="args">The progress event arguments.</param>
	private void Progress(FileTransferProgressEventArgs args)
	{
		if (_progressHandling >= 0 && WantsProgress)
		{
			_fileTransferProgress(this, args);
			if (args.Cancel)
			{
				_process.Cancel();
			}
		}
	}

	/// <summary>
	/// Determines and validates the path for the temporary XML log file, generating a unique name in
	/// the system's temp directory when no explicit path has been configured.
	/// </summary>
	/// <exception cref="SessionLocalException">Thrown when the configured XML log path already exists.</exception>
	private void SetupTempPath()
	{
		using (Logger.CreateCallstack())
		{
			if (!string.IsNullOrEmpty(_xmlLogPath))
			{
				bool flag = File.Exists(_xmlLogPath);
				Logger.WriteLine("Configured temporary file: {0} - Exists [{1}]", _xmlLogPath, flag);
				if (flag)
				{
					throw Logger.WriteException(new SessionLocalException(this, string.Format(CultureInfo.CurrentCulture, "Configured temporary file {0} already exists", new object[1] { _xmlLogPath })));
				}
				return;
			}
			string tempPath = Path.GetTempPath();
			Logger.WriteLine("Temporary folder: {0}", tempPath);
			string text = Process.GetCurrentProcess().Id.ToString("X4", CultureInfo.InvariantCulture);
			string text2 = GetHashCode().ToString("X8", CultureInfo.InvariantCulture);
			string text4;
			bool flag2;
			do
			{
				string text3 = ((_logUnique > 0) ? ("." + _logUnique.ToString(CultureInfo.InvariantCulture)) : string.Empty);
				_logUnique++;
				text4 = Path.Combine(tempPath, "wscp" + text + "." + text2 + text3 + ".tmp");
				flag2 = File.Exists(text4);
				Logger.WriteLine("Temporary file [{0}] - Exists [{1}]", text4, flag2);
			}
			while (flag2);
			_xmlLogPath = text4;
		}
	}

	/// <summary>
	/// Resolves the effective path to the WinSCP executable, checking the running process, the
	/// configured path, and the default search as fallbacks.
	/// </summary>
	/// <returns>The path to the WinSCP executable.</returns>
	private string GetExecutablePath()
	{
		if (_process != null)
		{
			return _process.ExecutablePath;
		}
		if (!string.IsNullOrEmpty(_executablePath))
		{
			return _executablePath;
		}
		return ExeSessionProcess.FindExecutable(this);
	}

	/// <summary>
	/// Validates and sets the session log file path, rejecting paths with an <c>.xml</c> extension.
	/// </summary>
	/// <param name="value">The path to the session log file.</param>
	/// <exception cref="ArgumentException">Thrown when <paramref name="value"/> has a <c>.xml</c> extension.</exception>
	private void SetSessionLogPath(string value)
	{
		CheckNotOpened();
		if (Path.GetExtension(value).Equals(".xml", StringComparison.OrdinalIgnoreCase))
		{
			throw Logger.WriteException(new ArgumentException("Session log cannot have .xml extension"));
		}
		_sessionLogPath = value;
	}

	/// <summary>
	/// Returns the <see cref="FieldInfo"/> for the field with the specified name, or <c>null</c> if not found.
	/// </summary>
	/// <param name="name">The name of the field to find.</param>
	/// <param name="bindingAttr">Binding flags that control the search.</param>
	/// <returns>The matching <see cref="FieldInfo"/>, or <c>null</c>.</returns>
	FieldInfo IReflect.GetField(string name, BindingFlags bindingAttr)
	{
		using (Logger.CreateCallstack())
		{
			Logger.WriteLine("Name [{0}]", name);
			FieldInfo field = GetType().GetField(name, bindingAttr);
			Logger.WriteLine("Result [{0}]", (field != null) ? field.Name : "null");
			return field;
		}
	}

	/// <summary>
	/// Returns all fields that match the specified binding flags.
	/// </summary>
	/// <param name="bindingAttr">Binding flags that control the search.</param>
	/// <returns>An array of <see cref="FieldInfo"/> objects representing matching fields.</returns>
	FieldInfo[] IReflect.GetFields(BindingFlags bindingAttr)
	{
		using (Logger.CreateCallstack())
		{
			FieldInfo[] fields = GetType().GetFields(bindingAttr);
			Logger.WriteLine("Fields [{0}]", fields.Length);
			return fields;
		}
	}

	/// <summary>
	/// Returns all members with the specified name that match the binding flags.
	/// </summary>
	/// <param name="name">The name of the member to find.</param>
	/// <param name="bindingAttr">Binding flags that control the search.</param>
	/// <returns>An array of <see cref="MemberInfo"/> objects for matching members.</returns>
	MemberInfo[] IReflect.GetMember(string name, BindingFlags bindingAttr)
	{
		using (Logger.CreateCallstack())
		{
			Logger.WriteLine("Name [{0}]", name);
			MemberInfo[] member = GetType().GetMember(name, bindingAttr);
			Logger.WriteLine("Result [{0}]", member.Length);
			return member;
		}
	}

	/// <summary>
	/// Returns all members that match the specified binding flags.
	/// </summary>
	/// <param name="bindingAttr">Binding flags that control the search.</param>
	/// <returns>An array of <see cref="MemberInfo"/> objects for all matching members.</returns>
	MemberInfo[] IReflect.GetMembers(BindingFlags bindingAttr)
	{
		using (Logger.CreateCallstack())
		{
			MemberInfo[] members = GetType().GetMembers(bindingAttr);
			Logger.WriteLine("Result [{0}]", members.Length);
			return members;
		}
	}

	/// <summary>
	/// Returns the <see cref="MethodInfo"/> for the method with the specified name, or <c>null</c> if not found.
	/// </summary>
	/// <param name="name">The name of the method to find.</param>
	/// <param name="bindingAttr">Binding flags that control the search.</param>
	/// <returns>The matching <see cref="MethodInfo"/>, or <c>null</c>.</returns>
	MethodInfo IReflect.GetMethod(string name, BindingFlags bindingAttr)
	{
		using (Logger.CreateCallstack())
		{
			Logger.WriteLine("Name [{0}]", name);
			MethodInfo method = GetType().GetMethod(name, bindingAttr);
			Logger.WriteLine("Result [{0}]", (method != null) ? method.Name : "null");
			return method;
		}
	}

	/// <summary>
	/// Returns the <see cref="MethodInfo"/> for the method matching the specified signature, or <c>null</c> if not found.
	/// </summary>
	/// <param name="name">The name of the method to find.</param>
	/// <param name="bindingAttr">Binding flags that control the search.</param>
	/// <param name="binder">An object that provides custom binding; pass <c>null</c> for the default binder.</param>
	/// <param name="types">An array of <see cref="Type"/> objects representing the parameter types.</param>
	/// <param name="modifiers">An array of parameter modifiers.</param>
	/// <returns>The matching <see cref="MethodInfo"/>, or <c>null</c>.</returns>
	MethodInfo IReflect.GetMethod(string name, BindingFlags bindingAttr, Binder binder, Type[] types, ParameterModifier[] modifiers)
	{
		using (Logger.CreateCallstack())
		{
			Logger.WriteLine("Name [{0}]", name);
			MethodInfo method = GetType().GetMethod(name, bindingAttr, binder, types, modifiers);
			Logger.WriteLine("Result [{0}]", (method != null) ? method.Name : "null");
			return method;
		}
	}

	/// <summary>
	/// Returns all methods that match the specified binding flags.
	/// </summary>
	/// <param name="bindingAttr">Binding flags that control the search.</param>
	/// <returns>An array of <see cref="MethodInfo"/> objects for all matching methods.</returns>
	MethodInfo[] IReflect.GetMethods(BindingFlags bindingAttr)
	{
		using (Logger.CreateCallstack())
		{
			MethodInfo[] methods = GetType().GetMethods(bindingAttr);
			Logger.WriteLine("Result [{0}]", methods.Length);
			return methods;
		}
	}

	/// <summary>
	/// Returns all properties that match the specified binding flags.
	/// </summary>
	/// <param name="bindingAttr">Binding flags that control the search.</param>
	/// <returns>An array of <see cref="PropertyInfo"/> objects for all matching properties.</returns>
	PropertyInfo[] IReflect.GetProperties(BindingFlags bindingAttr)
	{
		using (Logger.CreateCallstack())
		{
			PropertyInfo[] properties = GetType().GetProperties(bindingAttr);
			Logger.WriteLine("Result [{0}]", properties.Length);
			return properties;
		}
	}

	/// <summary>
	/// Returns the <see cref="PropertyInfo"/> matching the specified name and signature, or <c>null</c> if not found.
	/// </summary>
	/// <param name="name">The name of the property to find.</param>
	/// <param name="bindingAttr">Binding flags that control the search.</param>
	/// <param name="binder">An object that provides custom binding; pass <c>null</c> for the default binder.</param>
	/// <param name="returnType">The return type of the property.</param>
	/// <param name="types">An array of <see cref="Type"/> objects representing the indexer parameter types.</param>
	/// <param name="modifiers">An array of parameter modifiers.</param>
	/// <returns>The matching <see cref="PropertyInfo"/>, or <c>null</c>.</returns>
	PropertyInfo IReflect.GetProperty(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers)
	{
		using (Logger.CreateCallstack())
		{
			Logger.WriteLine("Name [{0}]", name);
			PropertyInfo property = GetType().GetProperty(name, bindingAttr, binder, returnType, types, modifiers);
			Logger.WriteLine("Result [{0}]", (property != null) ? property.Name : "null");
			return property;
		}
	}

	/// <summary>
	/// Returns the <see cref="PropertyInfo"/> for the property with the specified name, or <c>null</c> if not found.
	/// </summary>
	/// <param name="name">The name of the property to find.</param>
	/// <param name="bindingAttr">Binding flags that control the search.</param>
	/// <returns>The matching <see cref="PropertyInfo"/>, or <c>null</c>.</returns>
	PropertyInfo IReflect.GetProperty(string name, BindingFlags bindingAttr)
	{
		using (Logger.CreateCallstack())
		{
			Logger.WriteLine("Name [{0}]", name);
			PropertyInfo property = GetType().GetProperty(name, bindingAttr);
			Logger.WriteLine("Result [{0}]", (property != null) ? property.Name : "null");
			return property;
		}
	}

	/// <summary>
	/// Writes detailed diagnostic information about an <see cref="IReflect.InvokeMember"/> call to the session logger.
	/// </summary>
	/// <param name="name">The name of the member being invoked.</param>
	/// <param name="invokeAttr">The binding flags used for the invocation.</param>
	/// <param name="binder">The binder object, or <c>null</c>.</param>
	/// <param name="target">The object on which the member is invoked.</param>
	/// <param name="args">The argument array passed to the member, or <c>null</c>.</param>
	/// <param name="modifiers">Parameter modifier array, or <c>null</c>.</param>
	/// <param name="culture">The culture used for type coercion.</param>
	/// <param name="namedParameters">Named parameter names, or <c>null</c>.</param>
	private void LogInvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters)
	{
		Logger.WriteLine("Name [{0}]", name);
		Logger.WriteLine("BindingFlags [{0}]", invokeAttr);
		Logger.WriteLine("Binder [{0}]", binder);
		Logger.WriteLine("Target [{0}]", target);
		if (args != null)
		{
			Logger.WriteLine("Args [{0}] [{1}]", args.Length, (modifiers != null) ? modifiers.Length.ToString(CultureInfo.InvariantCulture) : "null");
			for (int i = 0; i < args.Length; i++)
			{
				Logger.WriteLine("Arg [{0}] [{1}] [{1}] [{2}]", i, args[i], (args[i] != null) ? args[i].GetType().ToString() : "null", (modifiers != null) ? modifiers[i].ToString() : "null");
			}
		}
		Logger.WriteLine("Culture [{0}]", culture);
		if (namedParameters != null)
		{
			foreach (string text in namedParameters)
			{
				Logger.WriteLine("NamedParameter [{0}]", text);
			}
		}
	}

	/// <summary>
	/// Invokes the specified member on the target object, with special handling to supply default values
	/// for optional parameters and to resolve ambiguous method overloads by matching parameter count.
	/// </summary>
	/// <param name="name">The name of the member to invoke.</param>
	/// <param name="invokeAttr">Binding flags that control the invocation.</param>
	/// <param name="binder">An object that provides custom binding; pass <c>null</c> for the default binder.</param>
	/// <param name="target">The object on which to invoke the member.</param>
	/// <param name="args">The argument array to pass to the member.</param>
	/// <param name="modifiers">Parameter modifier array, or <c>null</c>.</param>
	/// <param name="culture">The culture used for type coercion.</param>
	/// <param name="namedParameters">Named parameter names, or <c>null</c>.</param>
	/// <returns>The return value of the invoked member, or <c>null</c> for void members.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="target"/> or <paramref name="args"/> is <c>null</c>.</exception>
	object IReflect.InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters)
	{
		using (Logger.CreateCallstack())
		{
			object obj;
			try
			{
				bool logging = Logger.Logging;
				if (logging)
				{
					LogInvokeMember(name, invokeAttr, binder, target, args, modifiers, culture, namedParameters);
				}
				if (target == null)
				{
					throw Logger.WriteException(new ArgumentNullException("target"));
				}
				Type type = target.GetType();
				MethodInfo methodInfo = null;
				PropertyInfo propertyInfo = null;
				if (namedParameters == null)
				{
					try
					{
						BindingFlags bindingAttr = invokeAttr | BindingFlags.Instance | BindingFlags.Public;
						methodInfo = type.GetMethod(name, bindingAttr);
						if (args == null)
						{
							throw Logger.WriteException(new ArgumentNullException("args"));
						}
						if (methodInfo != null)
						{
							ParameterInfo[] parameters = methodInfo.GetParameters();
							if (args.Length < parameters.Length)
							{
								Logger.WriteLine("Provided less parameters [{0}] than defined [{1}]", args.Length, parameters.Length);
								object[] array = new object[parameters.Length];
								for (int i = 0; i < parameters.Length; i++)
								{
									if (i < args.Length)
									{
										array[i] = args[i];
										continue;
									}
									if (!parameters[i].IsOptional)
									{
										Logger.WriteLine("Parameter [{0}] of [{1}] is not optional", i, methodInfo);
										array = null;
										break;
									}
									Logger.WriteLine("Adding default value [{0}] for optional parameter [{1}]", parameters[i].DefaultValue, i);
									array[i] = parameters[i].DefaultValue;
								}
								if (array != null)
								{
									args = array;
								}
							}
						}
						else if (args.Length == 1)
						{
							propertyInfo = type.GetProperty(name, bindingAttr);
						}
					}
					catch (AmbiguousMatchException ex)
					{
						Logger.WriteLine("Unexpected ambiguous match [{0}]", ex.Message);
					}
				}
				if (methodInfo != null)
				{
					Logger.WriteLine("Invoking unambiguous method [{0}]", methodInfo);
					obj = methodInfo.Invoke(target, invokeAttr, binder, args, culture);
				}
				else if (propertyInfo != null)
				{
					Logger.WriteLine("Setting unambiguous property [{0}]", propertyInfo);
					propertyInfo.SetValue(target, args[0], invokeAttr, binder, null, culture);
					obj = null;
				}
				else
				{
					Logger.WriteLine("Invoking ambiguous/non-existing method 2 [{0}]", name);
					obj = type.InvokeMember(name, invokeAttr, binder, target, args, modifiers, culture, namedParameters);
				}
				if (!logging && Logger.Logging)
				{
					Logger.WriteLine("Invoking member {0} enabled debug logging", name);
				}
				Logger.WriteLine("Result [{0}] [{1}]", obj, (obj != null) ? obj.GetType().ToString() : "null");
			}
			catch (Exception ex2)
			{
				Logger.WriteLine("Error [{0}]", ex2);
				throw;
			}
			return obj;
		}
	}
}
