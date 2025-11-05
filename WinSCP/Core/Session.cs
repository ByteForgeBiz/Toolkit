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

[Guid("56FFC5CE-3867-4EF0-A3B5-CFFBEB99EA35")]
[ClassInterface(ClassInterfaceType.AutoDispatch)]
[ComVisible(true)]
[ComSourceInterfaces(typeof(ISessionEvents))]
public sealed class Session : IDisposable, IReflect
{
	internal const string Namespace = "http://winscp.net/schema/session/1.0";

	private ExeSessionProcess _process;

	private DateTime _lastOutput;

	private ElementLogReader _reader;

	private SessionLogReader _logReader;

	private readonly IList<OperationResultBase> _operationResults;

	private readonly IList<Action> _events;

	private AutoResetEvent _eventsEvent;

	private ManualResetEvent _choiceEvent;

	private bool _disposed;

	private string _executablePath;

	private string _additionalExecutableArguments;

	private bool _defaultConfiguration;

	private bool _disableVersionCheck;

	private string _iniFilePath;

	private TimeSpan _reconnectTime;

	private string _sessionLogPath;

	private bool _aborted;

	private int _logUnique;

	private string _xmlLogPath;

	private FileTransferProgressEventHandler _fileTransferProgress;

	private int _progressHandling;

	private bool _guardProcessWithJob;

	private string _homePath;

	private string _executableProcessUserName;

	private SecureString _executableProcessPassword;

	private StringCollection _error;

	private bool _ignoreFailed;

	private TimeSpan _sessionTimeout;

	private QueryReceivedEventHandler _queryReceived;

	private bool _throwStdOut;

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

	public bool XmlLogPreserve { get; set; }

	public string HomePath
	{
		get
		{
			CheckOpened();
			return _homePath;
		}
	}

	public TimeSpan Timeout { get; set; }

	public StringCollection Output { get; private set; }

	public bool Opened
	{
		get
		{
			CheckNotDisposed();
			return _process != null;
		}
	}

	internal bool WantsProgress => _fileTransferProgress != null;

	Type IReflect.UnderlyingSystemType => GetType();

	internal Logger Logger { get; private set; }

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

	internal bool TestHandlesClosedInternal { get; set; }

	internal Dictionary<string, string> RawConfiguration { get; private set; }

	internal bool DefaultConfigurationInternal => _defaultConfiguration;

	internal string IniFilePathInternal => _iniFilePath;

	public event FileTransferredEventHandler FileTransferred;

	public event FailedEventHandler Failed;

	public event OutputDataReceivedEventHandler OutputDataReceived;

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

	private void ResetOutput()
	{
		Output = new StringCollection();
		_error = new StringCollection();
	}

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

	public void Abort()
	{
		using (Logger.CreateCallstack())
		{
			CheckOpened();
			_aborted = true;
			_process?.Abort();
		}
	}

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

	private void SendOptionBatchCommand()
	{
		string command = string.Format(CultureInfo.InvariantCulture, "option batch {0}", new object[1] { (_queryReceived != null) ? "off" : "on" });
		WriteCommand(command);
	}

	private void WaitForGroup()
	{
		using ElementLogReader reader = _reader.WaitForGroupAndCreateLogReader();
		ReadElement(reader, LogReadFlags.ThrowFailures);
	}

	internal string GetErrorOutputMessage()
	{
		string result = null;
		if (_error.Count > 0)
		{
			result = string.Format(CultureInfo.CurrentCulture, "Error output was \"{0}\". ", new object[1] { ListToString(_error) });
		}
		return result;
	}

	private static string ListToString(StringCollection list)
	{
		string[] array = new string[list.Count];
		list.CopyTo(array, 0);
		return string.Join(Environment.NewLine, array);
	}

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

	private static string NormalizeIdent(string algorithm)
	{
		return algorithm.Replace("-", string.Empty);
	}

	public void Close()
	{
		using (CreateCallstackAndLock())
		{
			CheckOpened();
			Cleanup();
		}
	}

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

	public IEnumerable<RemoteFileInfo> EnumerateRemoteFiles(string path, string mask, EnumerationOptions options)
	{
		using (CreateCallstackAndLock())
		{
			CheckOpened();
			Regex regex = MaskToRegex(mask);
			return new ImplicitEnumerable<RemoteFileInfo>(DoEnumerateRemoteFiles(path, regex, options, throwReadErrors: true));
		}
	}

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

	public TransferOperationResult PutFiles(string localPath, string remotePath, bool remove = false, TransferOptions options = null)
	{
		using (CreateCallstackAndLock())
		{
			return DoPutFiles(localPath, remotePath, remove, options);
		}
	}

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

	private void AddTransfer(TransferOperationResult result, TransferEventArgs args)
	{
		if (args != null)
		{
			result.AddTransfer(args);
			RaiseFileTransferredEvent(args);
		}
	}

	public TransferOperationResult GetFiles(string remotePath, string localPath, bool remove = false, TransferOptions options = null)
	{
		using (CreateCallstackAndLock())
		{
			return DoGetFiles(remotePath, localPath, remove, options, string.Empty);
		}
	}

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

	private void StartGetCommand(string remotePath, string localPath, bool remove, TransferOptions options, string additionalParams)
	{
		if (options == null)
		{
			options = new TransferOptions();
		}
		CheckOpened();
		WriteCommand(string.Format(CultureInfo.InvariantCulture, "get {0} {1} {2} -- \"{3}\" \"{4}\"", BooleanSwitch(remove, "delete"), options.ToSwitches(), additionalParams, Tools.ArgumentEscape(remotePath), Tools.ArgumentEscape(localPath)));
	}

	public TransferOperationResult GetFilesToDirectory(string remoteDirectory, string localDirectory, string filemask = null, bool remove = false, TransferOptions options = null)
	{
		using (CreateCallstackAndLock())
		{
			return DoGetFilesToDirectory(remoteDirectory, localDirectory, filemask, remove, options, null);
		}
	}

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

	public TransferEventArgs GetFileToDirectory(string remoteFilePath, string localDirectory, bool remove = false, TransferOptions options = null)
	{
		using (CreateCallstackAndLock())
		{
			TransferOperationResult entryToDirectory = GetEntryToDirectory(remoteFilePath, localDirectory, remove, options, "-onlyfile");
			return GetOnlyFileOperation(entryToDirectory.Transfers);
		}
	}

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

	private void ParseRemotePath(string remoteFilePath, out string remoteDirectory, out string filemask)
	{
		if (string.IsNullOrEmpty(remoteFilePath))
		{
			throw Logger.WriteException(new ArgumentException("File to path cannot be empty", "remoteFilePath"));
		}
		remoteDirectory = RemotePath.GetDirectoryName(remoteFilePath);
		filemask = RemotePath.EscapeFileMask(RemotePath.GetFileName(remoteFilePath));
	}

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

	public RemovalOperationResult RemoveFiles(string path)
	{
		using (CreateCallstackAndLock())
		{
			return DoRemoveFiles(path, string.Empty);
		}
	}

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

	public RemovalEventArgs RemoveFile(string path)
	{
		using (CreateCallstackAndLock())
		{
			RemovalOperationResult removalOperationResult = RemoveEntry(path, "-onlyfile");
			return GetOnlyFileOperation(removalOperationResult.Removals);
		}
	}

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

	public SynchronizationResult SynchronizeDirectories(SynchronizationMode mode, string localPath, string remotePath, bool removeFiles, bool mirror = false, SynchronizationCriteria criteria = SynchronizationCriteria.Time, TransferOptions options = null)
	{
		using (CreateCallstackAndLock())
		{
			DoSynchronizeDirectories(mode, localPath, remotePath, removeFiles, mirror, criteria, options, string.Empty);
			return ReadSynchronizeDirectories();
		}
	}

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

	public ComparisonDifferenceCollection CompareDirectories(SynchronizationMode mode, string localPath, string remotePath, bool removeFiles, bool mirror = false, SynchronizationCriteria criteria = SynchronizationCriteria.Time, TransferOptions options = null)
	{
		using (CreateCallstackAndLock())
		{
			DoSynchronizeDirectories(mode, localPath, remotePath, removeFiles, mirror, criteria, options, "-preview");
			return ReadCompareDirectories(localPath, remotePath);
		}
	}

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

	public RemoteFileInfo GetFileInfo(string path)
	{
		using (CreateCallstackAndLock())
		{
			CheckOpened();
			return DoGetFileInfo(path);
		}
	}

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

	public bool FileExists(string path)
	{
		RemoteFileInfo fileInfo;
		return TryGetFileInfo(path, out fileInfo);
	}

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

	[Obsolete("Use RemotePath.EscapeFileMask")]
	public string EscapeFileMask(string fileMask)
	{
		return RemotePath.EscapeFileMask(fileMask);
	}

	[Obsolete("Use RemotePath.TranslateRemotePathToLocal")]
	public string TranslateRemotePathToLocal(string remotePath, string remoteRoot, string localRoot)
	{
		return RemotePath.TranslateRemotePathToLocal(remotePath, remoteRoot, localRoot);
	}

	[Obsolete("Use RemotePath.TranslateLocalPathToRemote")]
	public string TranslateLocalPathToRemote(string localPath, string localRoot, string remoteRoot)
	{
		return RemotePath.TranslateLocalPathToRemote(localPath, localRoot, remoteRoot);
	}

	[Obsolete("Use RemotePath.CombinePaths")]
	public string CombinePaths(string path1, string path2)
	{
		return RemotePath.Combine(path1, path2);
	}

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
    
	[ComUnregisterFunction]
	private static void ComUnregister(Type t)
	{
		RegistryKey classesRoot = Registry.ClassesRoot;
		classesRoot.DeleteSubKey(GetTypeLibKey(t), throwOnMissingSubKey: false);
		classesRoot.DeleteSubKey(GetVersionKey(t), throwOnMissingSubKey: false);
	}

	private static string GetClsidKey(Type t)
	{
		return "CLSID\\" + t.GUID.ToString("B").ToUpperInvariant();
	}

	private static string GetTypeLibKey(Type t)
	{
		return GetClsidKey(t) + "\\TypeLib";
	}

	private static string GetVersionKey(Type t)
	{
		return GetClsidKey(t) + "\\Version";
	}

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

	internal static string BooleanSwitch(bool flag, string name)
	{
		if (!flag)
		{
			return null;
		}
		return string.Format(CultureInfo.InvariantCulture, "-{0}", new object[1] { name });
	}

	internal static string BooleanSwitch(bool flag, string onName, string offName)
	{
		if (!flag)
		{
			return string.Format(CultureInfo.InvariantCulture, "-{0}", new object[1] { offName });
		}
		return string.Format(CultureInfo.InvariantCulture, "-{0}", new object[1] { onName });
	}

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

	private static string IncludeTrailingSlash(string path)
	{
		if (!string.IsNullOrEmpty(path) && !path.EndsWith("/", StringComparison.Ordinal))
		{
			path += "/";
		}
		return path;
	}

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

	private void WriteCommand(string command)
	{
		WriteCommand(command, command);
	}

	private void WriteCommand(string command, string log)
	{
		Logger.WriteLine("Command: [{0}]", log);
		_process.ExecuteCommand(command, log);
		GotOutput();
	}

	private static void ReadElement(CustomLogReader reader, LogReadFlags flags)
	{
		while (reader.Read(flags))
		{
		}
	}

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

	internal static string GenerateInMemoryPrivateKey(SessionOptions sessionOptions)
	{
		return BitConverter.ToString(Encoding.Default.GetBytes(sessionOptions.SshPrivateKey)).Replace("-", "");
	}

	private static string AddStarToList(string list)
	{
		return (string.IsNullOrEmpty(list) ? string.Empty : (list + ";")) + "*";
	}

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

	internal static string FormatSwitch(string key)
	{
		return string.Format(CultureInfo.InvariantCulture, "-{0}", new object[1] { key });
	}

	internal static string FormatSwitch(string key, string value)
	{
		return string.Format(CultureInfo.InvariantCulture, "-{0}=\"{1}\"", new object[2]
		{
			key,
			Tools.ArgumentEscape(value)
		});
	}

	internal static string FormatSwitch(string key, int value)
	{
		return string.Format(CultureInfo.InvariantCulture, "-{0}={1}", new object[2]
		{
			key,
			value.ToString(CultureInfo.InvariantCulture)
		});
	}

	internal static string FormatSwitch(string key, bool value)
	{
		return FormatSwitch(key, value ? 1 : 0);
	}

	private static string UriEscape(string s)
	{
		return Uri.EscapeDataString(s);
	}

	internal void GotOutput()
	{
		_lastOutput = DateTime.Now;
	}

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

	private void ScheduleEvent(Action action)
	{
		lock (_events)
		{
			_events.Add(action);
			_eventsEvent.Set();
		}
	}

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

	private void RaiseFileTransferredEvent(TransferEventArgs args)
	{
		Logger.WriteLine("FileTransferredEvent: [{0}]", args.FileName);
		this.FileTransferred?.Invoke(this, args);
	}

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

	private void CheckOpened()
	{
		if (!Opened)
		{
			throw Logger.WriteException(new InvalidOperationException("Session is not opened"));
		}
	}

	private void CheckNotOpened()
	{
		if (Opened)
		{
			throw Logger.WriteException(new InvalidOperationException("Session is already opened"));
		}
	}

	private void RaiseOutputDataReceived(string data, bool error)
	{
		Logger.WriteLine("Output: [{0}]", data);
		this.OutputDataReceived?.Invoke(this, new OutputDataReceivedEventArgs(data, error));
	}

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

	private IDisposable RegisterOperationResult(OperationResultBase operationResult)
	{
		_operationResults.Add(operationResult);
		return new OperationResultGuard(this, operationResult);
	}

	internal void UnregisterOperationResult(OperationResultBase operationResult)
	{
		_operationResults.Remove(operationResult);
	}

	private IDisposable CreateProgressHandler()
	{
		using (Logger.CreateCallstack())
		{
			_progressHandling++;
			return new ProgressHandler(this);
		}
	}

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

	private void Choice(QueryReceivedEventArgs args)
	{
		_queryReceived?.Invoke(this, args);
		_choiceEvent.Set();
	}

	internal void ProcessProgress(FileTransferProgressEventArgs args)
	{
		ScheduleEvent(delegate
		{
			Progress(args);
		});
	}

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

	private void SetSessionLogPath(string value)
	{
		CheckNotOpened();
		if (Path.GetExtension(value).Equals(".xml", StringComparison.OrdinalIgnoreCase))
		{
			throw Logger.WriteException(new ArgumentException("Session log cannot have .xml extension"));
		}
		_sessionLogPath = value;
	}

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

	FieldInfo[] IReflect.GetFields(BindingFlags bindingAttr)
	{
		using (Logger.CreateCallstack())
		{
			FieldInfo[] fields = GetType().GetFields(bindingAttr);
			Logger.WriteLine("Fields [{0}]", fields.Length);
			return fields;
		}
	}

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

	MemberInfo[] IReflect.GetMembers(BindingFlags bindingAttr)
	{
		using (Logger.CreateCallstack())
		{
			MemberInfo[] members = GetType().GetMembers(bindingAttr);
			Logger.WriteLine("Result [{0}]", members.Length);
			return members;
		}
	}

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

	MethodInfo[] IReflect.GetMethods(BindingFlags bindingAttr)
	{
		using (Logger.CreateCallstack())
		{
			MethodInfo[] methods = GetType().GetMethods(bindingAttr);
			Logger.WriteLine("Result [{0}]", methods.Length);
			return methods;
		}
	}

	PropertyInfo[] IReflect.GetProperties(BindingFlags bindingAttr)
	{
		using (Logger.CreateCallstack())
		{
			PropertyInfo[] properties = GetType().GetProperties(bindingAttr);
			Logger.WriteLine("Result [{0}]", properties.Length);
			return properties;
		}
	}

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
