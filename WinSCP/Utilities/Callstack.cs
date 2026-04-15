using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace ByteForge.WinSCP;

/// <summary>
/// Tracks method call entry and exit in the log, providing indented call-flow tracing
/// via the <see cref="IDisposable"/> pattern.
/// </summary>
internal class Callstack : IDisposable
{
	/// <summary>
	/// The logger used to write entry and exit messages.
	/// </summary>
	private readonly Logger _logger;

	/// <summary>
	/// The fully-qualified method name captured from the call stack, including an optional token.
	/// </summary>
	private readonly string _name;

	/// <summary>
	/// An optional token appended to the method name to distinguish concurrent or repeated calls.
	/// </summary>
	private readonly object _token;

	/// <summary>
	/// Initializes a new instance of the <see cref="Callstack"/> class, walking the call stack
	/// to determine the calling method and writing an "entering" log entry.
	/// </summary>
	/// <param name="logger">The logger to write call-flow entries to.</param>
	/// <param name="token">An optional token to append to the method name in log messages.</param>
	public Callstack(Logger logger, object token = null)
	{
		_logger = logger;
		if (!_logger.Logging)
		{
			return;
		}
		_token = token;
		Type typeFromHandle = typeof(Callstack);
		StackTrace stackTrace = new StackTrace();
		int num = 0;
		MethodBase methodBase;
		do
		{
			methodBase = stackTrace.GetFrame(num).GetMethod();
			if ((!methodBase.IsConstructor || !IsTypeOrSubType(methodBase.DeclaringType, typeFromHandle)) && (methodBase.MemberType != MemberTypes.Method || !IsTypeOrSubType(((MethodInfo)methodBase).ReturnType, typeFromHandle)))
			{
				break;
			}
			methodBase = null;
			num++;
		}
		while (num < stackTrace.FrameCount);
		if (methodBase != null)
		{
			_name = string.Format(CultureInfo.InvariantCulture, "{0}.{1}", new object[2]
			{
				methodBase.DeclaringType.Name,
				methodBase.Name
			});
			if (_token != null)
			{
				_name += string.Format(CultureInfo.InvariantCulture, "({0})", new object[1] { _token });
			}
			_logger.WriteLine("{0} entering", _name);
			_logger.Indent();
		}
	}

	/// <summary>
	/// Determines whether <paramref name="tested"/> is the same as or a subclass of <paramref name="type"/>.
	/// </summary>
	/// <param name="tested">The type to test.</param>
	/// <param name="type">The base or exact type to compare against.</param>
	/// <returns><see langword="true"/> if <paramref name="tested"/> equals or derives from <paramref name="type"/>; otherwise <see langword="false"/>.</returns>
	private static bool IsTypeOrSubType(Type tested, Type type)
	{
		if (!(tested == type))
		{
			return tested.IsSubclassOf(type);
		}
		return true;
	}

	/// <summary>
	/// Writes a "leaving" log entry and decreases the log indentation level.
	/// </summary>
	public virtual void Dispose()
	{
		if (_name != null)
		{
			_logger.Unindent();
			_logger.WriteLine("{0} leaving", _name);
		}
	}
}
