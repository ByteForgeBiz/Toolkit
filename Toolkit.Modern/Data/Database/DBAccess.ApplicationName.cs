using System.Diagnostics;
using System.Reflection;
#if NET48
using System.Data.SqlClient;
#else
using Microsoft.Data.SqlClient;
#endif

namespace ByteForge.Toolkit.Data;

public partial class DBAccess
{
    /// <summary>
    /// Adds the configured or inferred application name to SQL Client connection strings.
    /// </summary>
    private string GetConnectionStringWithApplicationName()
    {
        var connectionString = Options.GetConnectionString();

        if (!IsSqlClientDatabase)
            return connectionString;

        var builder = new SqlConnectionStringBuilder(connectionString);
        var configuredApplicationName = Options.ApplicationName?.Trim();

        if (!string.IsNullOrEmpty(configuredApplicationName))
            builder.ApplicationName = configuredApplicationName;
        else if (!builder.ShouldSerialize("Application Name") &&
                 !string.IsNullOrWhiteSpace(_defaultApplicationName))
            builder.ApplicationName = _defaultApplicationName;

        return builder.ConnectionString;
    }

    /// <summary>
    /// Resolves the friendly title of the application using the database helper.
    /// </summary>
    /// <remarks>
    /// Console and desktop applications normally expose an entry assembly. Classic
    /// ASP.NET applications may not, so the first non-Toolkit assembly on the current
    /// call stack is used as a fallback.
    /// </remarks>
    private static string ResolveApplicationName()
    {
        var toolkitAssembly = typeof(DBAccess).Assembly;
        var applicationAssembly = Assembly.GetEntryAssembly();

        if (applicationAssembly == null || applicationAssembly == toolkitAssembly)
        {
            applicationAssembly = new StackTrace()
                .GetFrames()?
                .Select(frame => frame.GetMethod()?.DeclaringType?.Assembly)
                .FirstOrDefault(assembly => assembly != null &&
                                            assembly != toolkitAssembly &&
                                            !IsFrameworkAssembly(assembly));
        }

        if (applicationAssembly == null)
            return "";

        var title = applicationAssembly
            .GetCustomAttribute<AssemblyTitleAttribute>()?
            .Title?
            .Trim();

        return !string.IsNullOrEmpty(title)
            ? title
            : applicationAssembly.GetName().Name ?? "";
    }

    /// <summary>
    /// Determines whether an assembly belongs to the runtime or common framework infrastructure.
    /// </summary>
    private static bool IsFrameworkAssembly(Assembly assembly)
    {
        var name = assembly.GetName().Name ?? "";

        return name == "mscorlib" ||
               name == "netstandard" ||
               name.StartsWith("System", StringComparison.Ordinal) ||
               name.StartsWith("Microsoft", StringComparison.Ordinal);
    }
}
