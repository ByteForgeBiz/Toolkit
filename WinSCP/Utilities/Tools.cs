using System;
using System.Collections.Generic;
using System.Globalization;

namespace ByteForge.WinSCP;

internal static class Tools
{
	public static int TimeSpanToMilliseconds(TimeSpan value)
	{
		if (value.TotalMilliseconds > 2147483647.0 || value.TotalMilliseconds < -2147483648.0)
		{
			throw new InvalidCastException(string.Format(CultureInfo.CurrentCulture, "Cannot convert {0} to integer", new object[1] { value }));
		}
		return (int)value.TotalMilliseconds;
	}

	public static TimeSpan MillisecondsToTimeSpan(int value)
	{
		return TimeSpan.FromMilliseconds(value);
	}

	public static string ArgumentEscape(string value)
	{
		for (int i = 0; i < value.Length; i++)
		{
			if (value[i] == '"')
			{
				value = value.Insert(i, "\"");
				i++;
			}
		}
		return value;
	}

	public static void AddRawParameters(ref string arguments, Dictionary<string, string> parameters, string switchName, bool count)
	{
		if (parameters.Count <= 0)
		{
			return;
		}
		if (!string.IsNullOrEmpty(arguments))
		{
			arguments += " ";
		}
		arguments += switchName;
		if (count)
		{
			arguments += string.Format(CultureInfo.InvariantCulture, "[{0}]", new object[1] { parameters.Count });
		}
		foreach (KeyValuePair<string, string> parameter in parameters)
		{
			arguments += string.Format(CultureInfo.InvariantCulture, " {0}=\"{1}\"", new object[2]
			{
				parameter.Key,
				ArgumentEscape(parameter.Value)
			});
		}
	}

	public static int LengthTo32Bit(long length)
	{
		if (length < int.MinValue || length > int.MaxValue)
		{
			throw new OverflowException(string.Format(CultureInfo.CurrentCulture, "Size {0} cannot be represented using 32-bit value", new object[1] { length }));
		}
		return (int)length;
	}
}
