using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace ByteForge.WinSCP;

/// <summary>
/// Provides logging functionality for debugging and troubleshooting WinSCP operations.
/// </summary>
internal class Logger : IDisposable
{
	/// <summary>
	/// The underlying text writer used to append log entries to the log file.
	/// <see langword="null"/> when logging is disabled.
	/// </summary>
	private StreamWriter _writter;

	/// <summary>
	/// The file path of the currently open log file, or <see langword="null"/> / empty when
	/// logging is disabled.
	/// </summary>
	private string _logPath;

	/// <summary>
	/// Maps each managed thread ID to its current indentation depth so that log entries
	/// from different threads are indented independently.
	/// </summary>
	private readonly Dictionary<int, int> _indents = new Dictionary<int, int>();

	/// <summary>
	/// Synchronisation object used to serialise all write operations to the log file.
	/// </summary>
	private readonly object _logLock = new object();

	/// <summary>
	/// The set of performance counters sampled at log open and periodically via
	/// <see cref="WriteCounters"/>.
	/// </summary>
	private readonly List<PerformanceCounter> _performanceCounters = new List<PerformanceCounter>();

	/// <summary>
	/// Backing field for <see cref="LogLevel"/>.
	/// </summary>
	private int _logLevel;

	/// <summary>
	/// Gets or sets the path to the log file.
	/// </summary>
	public string LogPath
	{
		get
		{
			return _logPath;
		}
		set
		{
			SetLogPath(value);
		}
	}

	/// <summary>
	/// Gets or sets the log level (-1 to 2, where higher values include more detailed logging).
	/// </summary>
	public int LogLevel
	{
		get
		{
			return _logLevel;
		}
		set
		{
			SetLogLevel(value);
		}
	}

	/// <summary>
	/// Gets a value indicating whether logging is currently active.
	/// </summary>
	public bool Logging
	{
		get
		{
			if (_writter != null)
			{
				return _writter.BaseStream.CanWrite;
			}
			return false;
		}
	}

	/// <summary>
	/// Gets the lock object for synchronizing logging operations.
	/// </summary>
	public Lock Lock { get; } = new Lock();

	/// <summary>
	/// Gets the file path of the executing assembly.
	/// </summary>
	/// <returns>The file path of the executing assembly, or null if it cannot be determined.</returns>
	public string GetAssemblyFilePath()
	{
		Assembly executingAssembly = Assembly.GetExecutingAssembly();
		return DoGetAssemblyFilePath(executingAssembly);
	}

	/// <summary>
	/// Gets the file path of the entry assembly.
	/// </summary>
	/// <returns>The file path of the entry assembly, or null if it cannot be determined.</returns>
	public string GetEntryAssemblyFilePath()
	{
		Assembly entryAssembly = Assembly.GetEntryAssembly();
		if (!(entryAssembly != null))
		{
			return null;
		}
		return DoGetAssemblyFilePath(entryAssembly);
	}

	/// <summary>
	/// Attempts to retrieve the <see cref="Assembly.CodeBase"/> of <paramref name="assembly"/>,
	/// catching <see cref="NotSupportedException"/> that may be thrown in some hosting environments.
	/// </summary>
	/// <param name="assembly">The assembly whose code base URI is requested.</param>
	/// <param name="e">
	/// When this method returns, contains the exception that was thrown, or <see langword="null"/>
	/// if the call succeeded.
	/// </param>
	/// <returns>The code base URI string, or <see langword="null"/> if it could not be retrieved.</returns>
	private string TryGetCodeBase(Assembly assembly, out Exception e)
	{
		try
		{
			e = null;
			return assembly.CodeBase;
		}
		catch (NotSupportedException ex)
		{
			e = ex;
			return null;
		}
	}

	/// <summary>
	/// Resolves the physical file path for <paramref name="assembly"/>, trying both
	/// <see cref="Assembly.CodeBase"/> and <see cref="Assembly.Location"/>.
	/// </summary>
	/// <param name="assembly">The assembly whose file path is to be resolved.</param>
	/// <returns>
	/// The absolute file path of the assembly, or <see langword="null"/> if it cannot be
	/// determined.
	/// </returns>
	private string DoGetAssemblyFilePath(Assembly assembly)
	{
		string text = null;
		Exception e;
		string text2 = TryGetCodeBase(assembly, out e);
		if (text2 == null)
		{
			if (e != null)
			{
				WriteLine("CodeBase not supported: " + e.Message);
			}
			text2 = string.Empty;
		}
		string location = assembly.Location;
		if (text2.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
		{
			text = text2.Substring("file://".Length).Replace('/', '\\');
			if (!string.IsNullOrEmpty(text))
			{
				text = ((text[0] != '\\') ? ("\\\\" + text) : text.Substring(1, text.Length - 1));
			}
		}
		if (string.IsNullOrEmpty(text) || !File.Exists(text))
		{
			if (File.Exists(location))
			{
				text = location;
			}
			else
			{
				WriteLine(string.Format(CultureInfo.CurrentCulture, "Cannot locate path of assembly [{0}] neither from its code base [{1}], nor from its location [{2}]", new object[3] { assembly, text2, location }));
				text = null;
			}
		}
		return text;
	}

	/// <summary>
	/// Enumerates available performance counter categories and registers CPU and memory
	/// counters for periodic logging via <see cref="WriteCounters"/>.
	/// </summary>
	private void CreateCounters()
	{
		try
		{
			PerformanceCounterCategory[] categories = PerformanceCounterCategory.GetCategories();
			foreach (PerformanceCounterCategory performanceCounterCategory in categories)
			{
				if (performanceCounterCategory.CategoryName == "Processor")
				{
					string[] instanceNames = performanceCounterCategory.GetInstanceNames();
					foreach (string instanceName in instanceNames)
					{
						AddCounter(new PerformanceCounter(performanceCounterCategory.CategoryName, "% Processor Time", instanceName));
					}
				}
			}
			AddCounter(new PerformanceCounter("Memory", "Available KBytes"));
		}
		catch (UnauthorizedAccessException)
		{
			WriteLine("Not authorized to get counters");
		}
		catch (Exception ex2)
		{
			WriteLine("Error getting counters: {0}", ex2);
		}
	}

	/// <summary>
	/// Primes <paramref name="counter"/> by calling <see cref="PerformanceCounter.NextValue"/>
	/// once (required before the first real read) and adds it to the tracked counter list.
	/// </summary>
	/// <param name="counter">The performance counter to register.</param>
	private void AddCounter(PerformanceCounter counter)
	{
		counter.NextValue();
		_performanceCounters.Add(counter);
	}

	/// <summary>
	/// Writes a line to the log file.
	/// </summary>
	/// <param name="line">The line to write.</param>
	public void WriteLine(string line)
	{
		lock (_logLock)
		{
			if (Logging)
			{
				DoWriteLine(line);
			}
		}
	}

	/// <summary>
	/// Writes a formatted line to the log file.
	/// </summary>
	/// <param name="format">The format string.</param>
	/// <param name="args">The format arguments.</param>
	public void WriteLine(string format, params object[] args)
	{
		lock (_logLock)
		{
			if (Logging)
			{
				DoWriteLine(string.Format(CultureInfo.CurrentCulture, format, args));
			}
		}
	}

	/// <summary>
	/// Writes a line to the log file if the specified log level is met.
	/// </summary>
	/// <param name="level">The minimum log level required to write the line.</param>
	/// <param name="line">The line to write.</param>
	public void WriteLineLevel(int level, string line)
	{
		if (LogLevel >= level)
		{
			WriteLine(line);
		}
	}

	/// <summary>
	/// Writes a formatted line to the log file if the specified log level is met.
	/// </summary>
	/// <param name="level">The minimum log level required to write the line.</param>
	/// <param name="line">The format string.</param>
	/// <param name="args">The format arguments.</param>
	public void WriteLineLevel(int level, string line, params object[] args)
	{
		if (LogLevel >= level)
		{
			WriteLine(line, args);
		}
	}

	/// <summary>
	/// Returns the managed thread ID of the currently executing thread, used as the key
	/// into the per-thread indentation dictionary.
	/// </summary>
	/// <returns>The managed thread ID of the current thread.</returns>
	private static int GetThread()
	{
		return Thread.CurrentThread.ManagedThreadId;
	}

	/// <summary>
	/// Increases the indentation level for subsequent log entries.
	/// </summary>
	public void Indent()
	{
		lock (_logLock)
		{
			int thread = GetThread();
			if (!_indents.TryGetValue(thread, out var value))
			{
				value = 0;
			}
			_indents[thread] = value + 1;
		}
	}

	/// <summary>
	/// Decreases the indentation level for subsequent log entries.
	/// </summary>
	public void Unindent()
	{
		lock (_logLock)
		{
			int thread = GetThread();
			_indents[thread]--;
		}
	}

	/// <summary>
	/// Releases all resources used by the logger.
	/// </summary>
	public void Dispose()
	{
		lock (_logLock)
		{
			if (Logging)
			{
				WriteCounters();
				WriteProcesses();
				_writter.Dispose();
				_writter = null;
			}
			foreach (PerformanceCounter performanceCounter in _performanceCounters)
			{
				performanceCounter.Dispose();
			}
		}
	}

	/// <summary>
	/// Writes performance counter values to the log.
	/// </summary>
	public void WriteCounters()
	{
		if (!Logging || LogLevel < 1)
		{
			return;
		}
		try
		{
			foreach (PerformanceCounter performanceCounter in _performanceCounters)
			{
				WriteLine("{0}{1}{2} = [{3}]", performanceCounter.CounterName, string.IsNullOrEmpty(performanceCounter.InstanceName) ? string.Empty : "/", performanceCounter.InstanceName, performanceCounter.NextValue());
			}
		}
		catch (Exception ex)
		{
			WriteLine("Error reading counters: {0}", ex);
		}
	}

	/// <summary>
	/// Writes current process information to the log.
	/// </summary>
	public void WriteProcesses()
	{
		if (!Logging || LogLevel < 1)
		{
			return;
		}
		try
		{
			Process[] processes = Process.GetProcesses();
			foreach (Process process in processes)
			{
				WriteLine("{0}:{1} - {2} - {3}", process.Id, process.ProcessName, GetProcessStartTime(process), GetTotalProcessorTime(process));
			}
		}
		catch (Exception ex)
		{
			WriteLine("Error logging processes: {0}", ex);
		}
	}

	/// <summary>
	/// Retrieves the start time of <paramref name="process"/>, returning the string
	/// <c>"???"</c> if access is denied or the process has already exited.
	/// </summary>
	/// <param name="process">The process whose start time is requested.</param>
	/// <returns>
	/// A <see cref="DateTime"/> boxed as <see cref="object"/> if available; otherwise the
	/// string <c>"???"</c>.
	/// </returns>
	private static object GetProcessStartTime(Process process)
	{
		try
		{
			return process.StartTime;
		}
		catch
		{
			return "???";
		}
	}

	/// <summary>
	/// Retrieves the total processor time used by <paramref name="process"/>, returning
	/// the string <c>"???"</c> if access is denied or the process has already exited.
	/// </summary>
	/// <param name="process">The process whose total processor time is requested.</param>
	/// <returns>
	/// A <see cref="TimeSpan"/> boxed as <see cref="object"/> if available; otherwise the
	/// string <c>"???"</c>.
	/// </returns>
	private static object GetTotalProcessorTime(Process process)
	{
		try
		{
			return process.TotalProcessorTime;
		}
		catch
		{
			return "???";
		}
	}

	/// <summary>
	/// Creates a callstack tracker for logging call flow.
	/// </summary>
	/// <param name="token">An optional token to identify the callstack.</param>
	/// <returns>A Callstack object for tracking call flow.</returns>
	public Callstack CreateCallstack(object token = null)
	{
		return new Callstack(this, token);
	}

	/// <summary>
	/// Creates a callstack and lock tracker for synchronized logging.
	/// </summary>
	/// <returns>A CallstackAndLock object for synchronized call flow tracking.</returns>
	public CallstackAndLock CreateCallstackAndLock()
	{
		return new CallstackAndLock(this, Lock);
	}

	/// <summary>
	/// Logs an exception with optional stack trace information.
	/// </summary>
	/// <param name="e">The exception to log.</param>
	/// <returns>The same exception that was passed in.</returns>
	public Exception WriteException(Exception e)
	{
		lock (_logLock)
		{
			if (Logging)
			{
				DoWriteLine(string.Format(CultureInfo.CurrentCulture, "Exception: {0}", new object[1] { e }));
				if (LogLevel >= 1)
				{
					DoWriteLine(new StackTrace().ToString());
				}
			}
		}
		return e;
	}

	/// <summary>
	/// Returns the current indentation depth for the calling thread.
	/// </summary>
	/// <returns>The number of indent levels active on the current thread, or 0 if none.</returns>
	private int GetIndent()
	{
		if (!_indents.TryGetValue(GetThread(), out var value))
		{
			return 0;
		}
		return value;
	}

	/// <summary>
	/// Formats <paramref name="message"/> with a timestamp, thread ID, and per-thread
	/// indentation, then writes it to the underlying <see cref="StreamWriter"/>.
	/// Must be called while <see cref="_logLock"/> is held.
	/// </summary>
	/// <param name="message">The message text to write.</param>
	private void DoWriteLine(string message)
	{
		int indent = GetIndent();
		string value = string.Format(CultureInfo.InvariantCulture, "[{0:yyyy-MM-dd HH:mm:ss.fff}] [{1:x4}] {2}{3}", DateTime.Now, Thread.CurrentThread.ManagedThreadId, (indent > 0) ? new string(' ', indent * 2) : string.Empty, message);
		_writter.WriteLine(value);
	}

	/// <summary>
	/// Changes the log file path, closing any existing log file and opening a new one.
	/// Writes environment information on open, and initialises performance counters when
	/// the log level is 1 or higher.
	/// </summary>
	/// <param name="value">The new log file path, or an empty string / <see langword="null"/> to disable logging.</param>
	private void SetLogPath(string value)
	{
		lock (_logLock)
		{
			if (!(_logPath != value))
			{
				return;
			}
			Dispose();
			_logPath = value;
			if (!string.IsNullOrEmpty(_logPath))
			{
				_writter = File.CreateText(_logPath);
				_writter.AutoFlush = true;
				WriteEnvironmentInfo();
				if (_logLevel >= 1)
				{
					CreateCounters();
				}
			}
		}
	}

	/// <summary>
	/// Writes a block of environment diagnostics to the log at session open, including
	/// assembly paths, OS version, bitness, time zone, user, runtime version, console
	/// encodings, and working directory.
	/// </summary>
	private void WriteEnvironmentInfo()
	{
		Assembly executingAssembly = Assembly.GetExecutingAssembly();
		WriteLine(".NET Framework build");
		WriteLine("Executing assembly: {0}", executingAssembly);
		Exception e;
		string text = TryGetCodeBase(executingAssembly, out e) ?? e?.Message ?? "unknown";
		WriteLine("Executing assembly codebase: {0}", text);
		WriteLine("Executing assembly location: {0}", executingAssembly.Location ?? "unknown");
		Assembly entryAssembly = Assembly.GetEntryAssembly();
		WriteLine("Entry Assembly: {0}", (entryAssembly != null) ? entryAssembly.ToString() : "unmanaged");
		WriteLine("Operating system: {0}", Environment.OSVersion);
		WriteLine("Bitness: {0}", Environment.Is64BitProcess ? "64-bit" : "32-bit");
		TimeSpan utcOffset = TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow);
		WriteLine("Timezone: {0}; {1}", ((utcOffset > TimeSpan.Zero) ? "+" : ((utcOffset < TimeSpan.Zero) ? "-" : string.Empty)) + utcOffset.ToString("hh\\:mm"), TimeZoneInfo.Local.IsDaylightSavingTime(DateTime.Now) ? TimeZoneInfo.Local.DaylightName : TimeZoneInfo.Local.StandardName);
		WriteLine("User: {0}@{1}@{2}; Interactive: {3}", Environment.UserName, Environment.UserDomainName, Environment.MachineName, Environment.UserInteractive);
		WriteLine("Runtime: {0}", Environment.Version);
		WriteLine("Console encoding: Input: {0} ({1}); Output: {2} ({3})", Console.InputEncoding.EncodingName, Console.InputEncoding.CodePage, Console.OutputEncoding.EncodingName, Console.OutputEncoding.CodePage);
		WriteLine("Working directory: {0}", Environment.CurrentDirectory);
		string assemblyFilePath = GetAssemblyFilePath();
		FileVersionInfo fileVersionInfo = (string.IsNullOrEmpty(assemblyFilePath) ? null : FileVersionInfo.GetVersionInfo(assemblyFilePath));
		WriteLine("Assembly path: {0}", assemblyFilePath);
		WriteLine("Assembly product version: {0}", (fileVersionInfo != null) ? fileVersionInfo.ProductVersion : "unknown");
		if (Assembly.GetEntryAssembly() != null)
		{
			WriteLine("Entry assembly path: {0}", GetEntryAssemblyFilePath());
		}
		WriteLine("Process path: " + GetProcessPath());
	}

	/// <summary>
	/// Gets the file path of the current process executable.
	/// </summary>
	/// <returns>The file path of the current process executable.</returns>
	public static string GetProcessPath()
	{
		return Process.GetCurrentProcess().MainModule?.FileName;
	}

	/// <summary>
	/// Gets the error message for the last Win32 error.
	/// </summary>
	/// <returns>The error message describing the last Win32 error.</returns>
	public static string LastWin32ErrorMessage()
	{
		return new Win32Exception(Marshal.GetLastWin32Error()).Message;
	}

	/// <summary>
	/// Validates and stores the new log level, throwing if the value is outside the
	/// permitted range of -1 to 2.
	/// </summary>
	/// <param name="value">The new log level (-1 to 2).</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is outside the range -1 to 2.</exception>
	private void SetLogLevel(int value)
	{
		if (value < -1 || value > 2)
		{
			throw WriteException(new ArgumentOutOfRangeException(string.Format(CultureInfo.CurrentCulture, "Logging level has to be in range -1 to 2")));
		}
		_logLevel = value;
	}
}
