using ByteForge.Toolkit.Configuration;
using System;

namespace ByteForge.Toolkit.Data;

/// <summary>
/// Creates database access instances for the configured database provider.
/// </summary>
public static class DatabaseAccessFactory
{
    /// <summary>
    /// Creates a database access instance using the selected database from configuration.
    /// </summary>
    /// <returns>A <see cref="DBAccess"/> instance for the selected database section.</returns>
    public static IDatabaseAccess Create()
    {
        var rootOptions = TryGetRootOptions();
        var dbSection = rootOptions?.SelectedDatabase ?? string.Empty;
        return Create(dbSection);
    }

    /// <summary>
    /// Creates a database access instance for the specified configuration section.
    /// </summary>
    /// <param name="dbSection">The configuration section that contains database options.</param>
    /// <param name="useDBAccess2">Ignored compatibility flag from the legacy toolkit.</param>
    /// <returns>A <see cref="DBAccess"/> instance for the requested database section.</returns>
    public static IDatabaseAccess Create(string dbSection, bool? useDBAccess2 = null)
    {
        if (string.IsNullOrEmpty(dbSection))
        {
            var rootOptions = TryGetRootOptions();
            dbSection = rootOptions?.SelectedDatabase ?? string.Empty;
        }

        if (string.IsNullOrEmpty(dbSection))
            throw new ArgumentException("The database section cannot be null or empty.", nameof(dbSection));

        var options = Configuration.Configuration.GetSection<DatabaseOptions>(dbSection);
        return Create(options, useDBAccess2);
    }

    /// <summary>
    /// Creates a database access instance for the specified database options.
    /// </summary>
    /// <param name="options">The database options to use.</param>
    /// <param name="useDBAccess2">Ignored compatibility flag from the legacy toolkit.</param>
    /// <returns>A <see cref="DBAccess"/> instance for the supplied options.</returns>
    public static IDatabaseAccess Create(DatabaseOptions options, bool? useDBAccess2 = null)
    {
        return new DBAccess(options ?? throw new ArgumentNullException(nameof(options)));
    }

    /// <summary>
    /// Creates the supported database access implementation explicitly.
    /// </summary>
    /// <param name="options">The database options to use.</param>
    /// <returns>A <see cref="DBAccess"/> instance for the supplied options.</returns>
    public static IDatabaseAccess CreateLegacy(DatabaseOptions options)
    {
        return Create(options);
    }

    /// <summary>
    /// Creates the supported database access implementation through the legacy DBAccess2 call shape.
    /// </summary>
    /// <param name="options">The database options to use.</param>
    /// <returns>A <see cref="DBAccess"/> instance for the supplied options.</returns>
    /// <remarks>
    /// DBAccess2 was intentionally not ported into the modern toolkit. This method remains only
    /// so callers that reference the old factory shape can compile while receiving DBAccess.
    /// </remarks>
    [Obsolete("DBAccess2 is not available in the modern toolkit. Use Create or CreateLegacy instead.")]
    public static IDatabaseAccess CreateModern(DatabaseOptions options)
    {
        return Create(options);
    }

    private static DatabaseRootOptions? TryGetRootOptions()
    {
        try
        {
            return Configuration.Configuration.GetSection<DatabaseRootOptions>("Data Source");
        }
        catch
        {
            return null;
        }
    }
}
