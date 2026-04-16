using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

/// <summary>
/// Provides utility methods for manipulating remote file paths and implements <see cref="IReflect"/> for COM interoperability.
/// </summary>
[Guid("70253534-C5DC-4EF3-9C98-65C57D79C324")]
[ClassInterface(ClassInterfaceType.AutoDispatch)]
[ComVisible(true)]
public sealed class RemotePath : IReflect
{
	/// <inheritdoc/>
	Type IReflect.UnderlyingSystemType => GetType();

	/// <summary>
	/// Escapes special characters in a file mask for remote operations.
	/// </summary>
	/// <param name="fileMask">The file mask to escape.</param>
	/// <returns>The escaped file mask.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="fileMask"/> is null.</exception>
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

	/// <summary>
	/// Escapes special characters in an operation mask for remote operations.
	/// </summary>
	/// <param name="operationMask">The operation mask to escape.</param>
	/// <returns>The escaped operation mask.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="operationMask"/> is null.</exception>
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

	/// <summary>
	/// Combines two paths into a single path. (Obsolete, use <see cref="Combine"/> instead.)
	/// </summary>
	/// <param name="path1">The first path.</param>
	/// <param name="path2">The second path.</param>
	/// <returns>The combined path.</returns>
	[Obsolete("Use RemotePath.Combine method")]
	public static string CombinePaths(string path1, string path2)
	{
		return Combine(path1, path2);
	}

	/// <summary>
	/// Combines two paths into a single path.
	/// </summary>
	/// <param name="path1">The first path.</param>
	/// <param name="path2">The second path.</param>
	/// <returns>The combined path.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="path1"/> or <paramref name="path2"/> is null.</exception>
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

	/// <summary>
	/// Translates a remote path to a local path using specified roots.
	/// </summary>
	/// <param name="remotePath">The remote path.</param>
	/// <param name="remoteRoot">The remote root directory.</param>
	/// <param name="localRoot">The local root directory.</param>
	/// <returns>The translated local path.</returns>
	/// <exception cref="ArgumentNullException">Thrown if any argument is null.</exception>
	/// <exception cref="InvalidOperationException">Thrown if <paramref name="remotePath"/> does not start with <paramref name="remoteRoot"/>.</exception>
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

	/// <summary>
	/// Adds a separator to the end of the path if not present.
	/// </summary>
	/// <param name="path">The path to modify.</param>
	/// <param name="separator">The separator to add.</param>
	/// <returns>The path with separator.</returns>
	private static string AddSeparator(string path, string separator)
	{
		if (path.Length > 0 && !path.EndsWith(separator, StringComparison.Ordinal))
		{
			path += separator;
		}
		return path;
	}

	/// <summary>
	/// Translates a local path to a remote path using specified roots.
	/// </summary>
	/// <param name="localPath">The local path.</param>
	/// <param name="localRoot">The local root directory.</param>
	/// <param name="remoteRoot">The remote root directory.</param>
	/// <returns>The translated remote path.</returns>
	/// <exception cref="ArgumentNullException">Thrown if any argument is null.</exception>
	/// <exception cref="InvalidOperationException">Thrown if <paramref name="localPath"/> does not start with <paramref name="localRoot"/>.</exception>
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

	/// <summary>
	/// Gets the directory name from a path.
	/// </summary>
	/// <param name="path">The path to analyze.</param>
	/// <returns>The directory name, or null if not applicable.</returns>
	/// <exception cref="ArgumentException">Thrown if <paramref name="path"/> is empty.</exception>
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

	/// <summary>
	/// Adds a directory separator to the end of the path if not present.
	/// </summary>
	/// <param name="path">The path to modify.</param>
	/// <returns>The path with a directory separator.</returns>
	/// <exception cref="ArgumentException">Thrown if <paramref name="path"/> is empty.</exception>
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

	/// <summary>
	/// Gets the file name from a path.
	/// </summary>
	/// <param name="path">The path to analyze.</param>
	/// <returns>The file name, or null if not applicable.</returns>
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

	// IReflect interface implementation
	/// <summary>
	/// Returns a <see cref="FieldInfo"/> for the field with the specified name, delegating
	/// to the underlying runtime type.
	/// </summary>
	/// <param name="name">The name of the field to find.</param>
	/// <param name="bindingAttr">A bitwise combination of <see cref="BindingFlags"/> values that control the search.</param>
	/// <returns>A <see cref="FieldInfo"/> for the field, or <see langword="null"/> if no matching field is found.</returns>
	FieldInfo IReflect.GetField(string name, BindingFlags bindingAttr)
	{
		return GetType().GetField(name, bindingAttr);
	}

	/// <summary>
	/// Returns all public fields of the underlying runtime type that match the specified binding flags.
	/// </summary>
	/// <param name="bindingAttr">A bitwise combination of <see cref="BindingFlags"/> values that control the search.</param>
	/// <returns>An array of <see cref="FieldInfo"/> objects representing the matching fields.</returns>
	FieldInfo[] IReflect.GetFields(BindingFlags bindingAttr)
	{
		return GetType().GetFields(bindingAttr);
	}

	/// <summary>
	/// Returns all members with the specified name from the underlying runtime type.
	/// </summary>
	/// <param name="name">The name of the member to find.</param>
	/// <param name="bindingAttr">A bitwise combination of <see cref="BindingFlags"/> values that control the search.</param>
	/// <returns>An array of <see cref="MemberInfo"/> objects representing the matching members.</returns>
	MemberInfo[] IReflect.GetMember(string name, BindingFlags bindingAttr)
	{
		return GetType().GetMember(name, bindingAttr);
	}

	/// <summary>
	/// Returns all members of the underlying runtime type that match the specified binding flags.
	/// </summary>
	/// <param name="bindingAttr">A bitwise combination of <see cref="BindingFlags"/> values that control the search.</param>
	/// <returns>An array of <see cref="MemberInfo"/> objects representing the matching members.</returns>
	MemberInfo[] IReflect.GetMembers(BindingFlags bindingAttr)
	{
		return GetType().GetMembers(bindingAttr);
	}

	/// <summary>
	/// Returns a <see cref="MethodInfo"/> for the method with the specified name, delegating
	/// to the underlying runtime type.
	/// </summary>
	/// <param name="name">The name of the method to find.</param>
	/// <param name="bindingAttr">A bitwise combination of <see cref="BindingFlags"/> values that control the search.</param>
	/// <returns>A <see cref="MethodInfo"/> for the method, or <see langword="null"/> if no match is found.</returns>
	MethodInfo IReflect.GetMethod(string name, BindingFlags bindingAttr)
	{
		return GetType().GetMethod(name, bindingAttr);
	}

	/// <summary>
	/// Returns a <see cref="MethodInfo"/> for the method matching the specified name,
	/// binder, parameter types, and modifiers.
	/// </summary>
	/// <param name="name">The name of the method to find.</param>
	/// <param name="bindingAttr">A bitwise combination of <see cref="BindingFlags"/> values that control the search.</param>
	/// <param name="binder">A <see cref="Binder"/> that defines a set of properties enabling binding.</param>
	/// <param name="types">An array of <see cref="Type"/> objects representing the parameter types.</param>
	/// <param name="modifiers">An array of <see cref="ParameterModifier"/> objects representing parameter modifiers.</param>
	/// <returns>A <see cref="MethodInfo"/> for the matching method, or <see langword="null"/> if no match is found.</returns>
	MethodInfo IReflect.GetMethod(string name, BindingFlags bindingAttr, Binder binder, Type[] types, ParameterModifier[] modifiers)
	{
		return GetType().GetMethod(name, bindingAttr, binder, types, modifiers);
	}

	/// <summary>
	/// Returns all methods of the underlying runtime type that match the specified binding flags.
	/// </summary>
	/// <param name="bindingAttr">A bitwise combination of <see cref="BindingFlags"/> values that control the search.</param>
	/// <returns>An array of <see cref="MethodInfo"/> objects representing the matching methods.</returns>
	MethodInfo[] IReflect.GetMethods(BindingFlags bindingAttr)
	{
		return GetType().GetMethods(bindingAttr);
	}

	/// <summary>
	/// Returns all properties of the underlying runtime type that match the specified binding flags.
	/// </summary>
	/// <param name="bindingAttr">A bitwise combination of <see cref="BindingFlags"/> values that control the search.</param>
	/// <returns>An array of <see cref="PropertyInfo"/> objects representing the matching properties.</returns>
	PropertyInfo[] IReflect.GetProperties(BindingFlags bindingAttr)
	{
		return GetType().GetProperties(bindingAttr);
	}

	/// <summary>
	/// Returns a <see cref="PropertyInfo"/> for the property matching the specified name,
	/// return type, parameter types, and modifiers.
	/// </summary>
	/// <param name="name">The name of the property to find.</param>
	/// <param name="bindingAttr">A bitwise combination of <see cref="BindingFlags"/> values that control the search.</param>
	/// <param name="binder">A <see cref="Binder"/> that defines a set of properties enabling binding.</param>
	/// <param name="returnType">The return type of the property.</param>
	/// <param name="types">An array of <see cref="Type"/> objects representing the indexer parameter types.</param>
	/// <param name="modifiers">An array of <see cref="ParameterModifier"/> objects representing parameter modifiers.</param>
	/// <returns>A <see cref="PropertyInfo"/> for the matching property, or <see langword="null"/> if no match is found.</returns>
	PropertyInfo IReflect.GetProperty(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers)
	{
		return GetType().GetProperty(name, bindingAttr, binder, returnType, types, modifiers);
	}

	/// <summary>
	/// Returns a <see cref="PropertyInfo"/> for the property with the specified name and
	/// binding flags, delegating to the underlying runtime type.
	/// </summary>
	/// <param name="name">The name of the property to find.</param>
	/// <param name="bindingAttr">A bitwise combination of <see cref="BindingFlags"/> values that control the search.</param>
	/// <returns>A <see cref="PropertyInfo"/> for the property, or <see langword="null"/> if no match is found.</returns>
	PropertyInfo IReflect.GetProperty(string name, BindingFlags bindingAttr)
	{
		return GetType().GetProperty(name, bindingAttr);
	}

	/// <summary>
	/// Invokes a member on the specified <paramref name="target"/> object using late binding.
	/// </summary>
	/// <param name="name">The name of the member to invoke.</param>
	/// <param name="invokeAttr">A bitwise combination of <see cref="BindingFlags"/> values that specifies the invocation type.</param>
	/// <param name="binder">A <see cref="Binder"/> that defines a set of properties enabling binding.</param>
	/// <param name="target">The object on which to invoke the member.</param>
	/// <param name="args">An array of arguments to pass to the member.</param>
	/// <param name="modifiers">An array of <see cref="ParameterModifier"/> objects representing parameter modifiers.</param>
	/// <param name="culture">The <see cref="CultureInfo"/> governing type coercion.</param>
	/// <param name="namedParameters">An array of parameter names corresponding to elements in <paramref name="args"/>.</param>
	/// <returns>The return value of the invoked member.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="target"/> is <see langword="null"/>.</exception>
	object IReflect.InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters)
	{
		if (target == null)
		{
			throw new ArgumentNullException("target");
		}
		return target.GetType().InvokeMember(name, invokeAttr, binder, target, args, modifiers, culture, namedParameters);
	}
}
