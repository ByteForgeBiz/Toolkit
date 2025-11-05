using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

[Guid("70253534-C5DC-4EF3-9C98-65C57D79C324")]
[ClassInterface(ClassInterfaceType.AutoDispatch)]
[ComVisible(true)]
public sealed class RemotePath : IReflect
{
	Type IReflect.UnderlyingSystemType => GetType();

	public static string EscapeFileMask(string fileMask)
	{
		if (fileMask == null)
		{
			throw new ArgumentNullException("fileMask");
		}
		int num = fileMask.LastIndexOf('/');
		string obj = ((num > 0) ? fileMask.Substring(0, num + 1) : string.Empty);
		string text = ((num > 0) ? fileMask.Substring(num + 1) : fileMask);
		text = text.Replace("[", "[[]").Replace("*", "[*]").Replace("?", "[?]")
			.Replace("<", "<<")
			.Replace(">", ">>");
		return obj + text;
	}

	public static string EscapeOperationMask(string operationMask)
	{
		if (operationMask == null)
		{
			throw new ArgumentNullException("operationMask");
		}
		int num = operationMask.LastIndexOf('/');
		string obj = ((num > 0) ? operationMask.Substring(0, num + 1) : string.Empty);
		string text = ((num > 0) ? operationMask.Substring(num + 1) : operationMask);
		text = text.Replace("\\", "\\\\").Replace("*", "\\*").Replace("?", "\\?");
		return obj + text;
	}

	[Obsolete("Use RemotePath.Combine method")]
	public static string CombinePaths(string path1, string path2)
	{
		return Combine(path1, path2);
	}

	public static string Combine(string path1, string path2)
	{
		if (path1 == null)
		{
			throw new ArgumentNullException("path1");
		}
		if (path2 == null)
		{
			throw new ArgumentNullException("path2");
		}
		if (path2.StartsWith("/", StringComparison.Ordinal))
		{
			return path2;
		}
		return path1 + ((path1.Length == 0 || path2.Length == 0 || path1.EndsWith("/", StringComparison.Ordinal)) ? string.Empty : "/") + path2;
	}

	public static string TranslateRemotePathToLocal(string remotePath, string remoteRoot, string localRoot)
	{
		if (remotePath == null)
		{
			throw new ArgumentNullException("remotePath");
		}
		if (remoteRoot == null)
		{
			throw new ArgumentNullException("remoteRoot");
		}
		if (localRoot == null)
		{
			throw new ArgumentNullException("localRoot");
		}
		localRoot = AddSeparator(localRoot, "\\");
		remoteRoot = AddSeparator(remoteRoot, "/");
		if (AddSeparator(remotePath, "/") == remoteRoot)
		{
			return localRoot;
		}
		if (!remotePath.StartsWith(remoteRoot, StringComparison.Ordinal))
		{
			throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "{0} does not start with {1}", new object[2] { remotePath, remoteRoot }));
		}
		string text = remotePath.Substring(remoteRoot.Length);
		if (text.StartsWith("/", StringComparison.Ordinal))
		{
			text = text.Substring(1);
		}
		text = text.Replace('/', '\\');
		return localRoot + text;
	}

	private static string AddSeparator(string path, string separator)
	{
		if (path.Length > 0 && !path.EndsWith(separator, StringComparison.Ordinal))
		{
			path += separator;
		}
		return path;
	}

	public static string TranslateLocalPathToRemote(string localPath, string localRoot, string remoteRoot)
	{
		if (localPath == null)
		{
			throw new ArgumentNullException("localPath");
		}
		if (localRoot == null)
		{
			throw new ArgumentNullException("localRoot");
		}
		if (remoteRoot == null)
		{
			throw new ArgumentNullException("remoteRoot");
		}
		localRoot = AddSeparator(localRoot, "\\");
		remoteRoot = AddSeparator(remoteRoot, "/");
		if (AddSeparator(localPath, "\\") == localRoot)
		{
			return remoteRoot;
		}
		if (!localPath.StartsWith(localRoot, StringComparison.Ordinal))
		{
			throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "{0} does not start with {1}", new object[2] { localPath, localRoot }));
		}
		string text = localPath.Substring(localRoot.Length);
		if (text.StartsWith("\\", StringComparison.Ordinal))
		{
			text = text.Substring(1);
		}
		text = text.Replace('\\', '/');
		return remoteRoot + text;
	}

	public static string GetDirectoryName(string path)
	{
		if (path == null)
		{
			return null;
		}
		if (path.Length == 0)
		{
			throw new ArgumentException("Path cannot be empty", "path");
		}
		int num = path.LastIndexOf('/');
		if (num < 0)
		{
			return string.Empty;
		}
		if (num == 0)
		{
			if (path.Length == 1)
			{
				return null;
			}
			return "/";
		}
		return path.Substring(0, num);
	}

	public static string AddDirectorySeparator(string path)
	{
		if (string.IsNullOrEmpty(path))
		{
			throw new ArgumentException("Path cannot be empty", "path");
		}
		if (!path.EndsWith("/", StringComparison.Ordinal))
		{
			path += "/";
		}
		return path;
	}

	public static string GetFileName(string path)
	{
		if (string.IsNullOrEmpty(path))
		{
			return null;
		}
		int num = path.LastIndexOf('/');
		if (num >= 0)
		{
			return path.Substring(num + 1);
		}
		return path;
	}

	FieldInfo IReflect.GetField(string name, BindingFlags bindingAttr)
	{
		return GetType().GetField(name, bindingAttr);
	}

	FieldInfo[] IReflect.GetFields(BindingFlags bindingAttr)
	{
		return GetType().GetFields(bindingAttr);
	}

	MemberInfo[] IReflect.GetMember(string name, BindingFlags bindingAttr)
	{
		return GetType().GetMember(name, bindingAttr);
	}

	MemberInfo[] IReflect.GetMembers(BindingFlags bindingAttr)
	{
		return GetType().GetMembers(bindingAttr);
	}

	MethodInfo IReflect.GetMethod(string name, BindingFlags bindingAttr)
	{
		return GetType().GetMethod(name, bindingAttr);
	}

	MethodInfo IReflect.GetMethod(string name, BindingFlags bindingAttr, Binder binder, Type[] types, ParameterModifier[] modifiers)
	{
		return GetType().GetMethod(name, bindingAttr, binder, types, modifiers);
	}

	MethodInfo[] IReflect.GetMethods(BindingFlags bindingAttr)
	{
		return GetType().GetMethods(bindingAttr);
	}

	PropertyInfo[] IReflect.GetProperties(BindingFlags bindingAttr)
	{
		return GetType().GetProperties(bindingAttr);
	}

	PropertyInfo IReflect.GetProperty(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers)
	{
		return GetType().GetProperty(name, bindingAttr, binder, returnType, types, modifiers);
	}

	PropertyInfo IReflect.GetProperty(string name, BindingFlags bindingAttr)
	{
		return GetType().GetProperty(name, bindingAttr);
	}

	object IReflect.InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters)
	{
		if (target == null)
		{
			throw new ArgumentNullException("target");
		}
		return target.GetType().InvokeMember(name, invokeAttr, binder, target, args, modifiers, culture, namedParameters);
	}
}
