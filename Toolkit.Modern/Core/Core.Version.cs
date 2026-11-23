using System.Reflection;

namespace ByteForge.Toolkit.Core;

public static partial class Core
{
    /// <summary>Reads the application's product version, falling back for older assemblies.</summary>
    /// <param name="assembly">The application assembly, rather than the shared toolkit assembly.</param>
    /// <returns>The informational version or numeric assembly version.</returns>
    public static string GetDisplayVersion(Assembly assembly)
    {
        if (assembly == null) throw new System.ArgumentNullException(nameof(assembly));
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        return string.IsNullOrWhiteSpace(version) ? assembly.GetName().Version?.ToString() ?? "Unknown" : version;
    }
}
