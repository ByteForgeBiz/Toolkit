using System.ComponentModel;
using System.Reflection;

namespace ByteForge.Toolkit;
/// <summary>
/// Provides helper methods for resolving default values for properties using attributes or type defaults.
/// </summary>
internal static class DefaultValueHelper
{
    /// <summary>
    /// Resolves the default value for a property, using <see cref="DefaultValueAttribute"/>, <see cref="DefaultValueProviderAttribute"/>, or type defaults.
    /// </summary>
    /// <param name="prop">The property for which to resolve the default value.</param>
    /// <returns>The resolved default value.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="prop"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if a non-nullable value type property has no default value.</exception>
    public static object? ResolveDefaultValue(PropertyInfo prop)
    {
        if (prop == null)
            throw new ArgumentNullException(nameof(prop));

        // Prefer DefaultValueAttribute if present
        var defaultAttr = prop.GetCustomAttribute<DefaultValueAttribute>();
        if (defaultAttr != null)
            // Allow null for reference types or nullable value types
            return ConvertDefaultValue(prop, defaultAttr.Value);

        var defaultProviderAttr = prop.GetCustomAttribute<DefaultValueProviderAttribute>();
        if (defaultProviderAttr != null)
            return ConvertDefaultValue(prop, defaultProviderAttr.GetDefaultValue());

        return HandleNullDefaultValue(prop);
    }

    /// <summary>
    /// Converts a default value to the property's type, if necessary.
    /// </summary>
    /// <param name="prop">The property for which the value is being converted.</param>
    /// <param name="value">The value to convert.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the value cannot be assigned to the property type.</exception>
    private static object? ConvertDefaultValue(PropertyInfo prop, object? value)
    {
        if (value == null)
            return HandleNullDefaultValue(prop);

        if (prop.PropertyType.IsInstanceOfType(value))
            return value;

        // Try to convert if possible (e.g., string to int)
        try
        {
            return Convert.ChangeType(value, prop.PropertyType);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Default value for property '{prop.Name}' cannot be assigned to {prop.PropertyType.FullName}.", ex);
        }
    }

    /// <summary>
    /// Handles the case where a property's default value is null.
    /// </summary>
    /// <param name="prop">The property for which to handle the null default value.</param>
    /// <returns>Null if the property is a reference type or nullable value type.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the property is a non-nullable value type.</exception>
    private static object? HandleNullDefaultValue(PropertyInfo prop)
    {
        if (!prop.PropertyType.IsValueType || Nullable.GetUnderlyingType(prop.PropertyType) != null)
            return null;

        var value = Activator.CreateInstance(prop.PropertyType);
        if (value != null)
            return value;

        throw new InvalidOperationException($"Property '{prop.Name}' is a non-nullable value type and cannot have a null default value.");
    }
}
