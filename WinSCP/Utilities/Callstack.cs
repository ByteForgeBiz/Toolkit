using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace ByteForge.WinSCP;

internal class Callstack : IDisposable
{
	private readonly Logger _logger;

	private readonly string _name;

	private readonly object _token;

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

	private static bool IsTypeOrSubType(Type tested, Type type)
	{
		if (!(tested == type))
		{
			return tested.IsSubclassOf(type);
		}
		return true;
	}

	public virtual void Dispose()
	{
		if (_name != null)
		{
			_logger.Unindent();
			_logger.WriteLine("{0} leaving", _name);
		}
	}
}
