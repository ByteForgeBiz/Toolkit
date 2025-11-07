namespace ByteForge.Toolkit.Utilities;
/*
 * __   __    _           ___                     _           ___          _    _            
 * \ \ / /_ _| |_  _ ___ / __|___ _ ___ _____ _ _| |_ ___ _ _| _ \___ __ _(_)__| |_ _ _ _  _ 
 *  \ V / _` | | || / -_) (__/ _ \ ' \ V / -_) '_|  _/ -_) '_|   / -_) _` | (_-<  _| '_| || |
 *   \_/\__,_|_|\_,_\___|\___\___/_||_\_/\___|_|  \__\___|_| |_|_\___\__, |_/__/\__|_|  \_, |
 *                                                                   |___/              |__/ 
 */
/// <summary>
/// Provides a registry for value converters used with <see cref="DBColumnAttribute"/>.
/// </summary>
public static class ValueConverterRegistry
{
    private static readonly Dictionary<string, Func<object, object>> _converters =
        new Dictionary<string, Func<object, object>>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers a converter with the specified name.
    /// </summary>
    /// <param name="name">The name of the converter.</param>
    /// <param name="converter">The converter function.</param>
    /// <exception cref="ArgumentNullException">Thrown when name or converter is null.</exception>
    /// <exception cref="ArgumentException">Thrown when a converter with the same name already exists.</exception>
    public static void RegisterConverter(string name, Func<object, object> converter)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException(nameof(name));

        if (converter == null)
            throw new ArgumentNullException(nameof(converter));

        if (_converters.ContainsKey(name) && converter.Method.Name != _converters[name].Method.Name)
            throw new ArgumentException($"A converter with the name '{name}' is already registered.", nameof(name));

        _converters[name] = converter;
    }

    /// <summary>
    /// Gets a converter with the specified name.
    /// </summary>
    /// <param name="name">The name of the converter.</param>
    /// <returns>The converter function, or null if no converter with the specified name exists.</returns>
    public static Func<object, object>? GetConverter(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        return _converters.TryGetValue(name!, out var converter) ? converter : null;
    }

    /// <summary>
    /// Determines whether a converter with the specified name exists.
    /// </summary>
    /// <param name="name">The name of the converter.</param>
    /// <returns>true if a converter with the specified name exists; otherwise, false.</returns>
    public static bool HasConverter(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        return _converters.ContainsKey(name);
    }

    /// <summary>
    /// Removes a converter with the specified name.
    /// </summary>
    /// <param name="name">The name of the converter.</param>
    /// <returns>true if the converter was removed; otherwise, false.</returns>
    public static bool RemoveConverter(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        return _converters.Remove(name);
    }

    /// <summary>
    /// Clears all registered converters.
    /// </summary>
    public static void ClearConverters()
    {
        _converters.Clear();
    }
}
