using ByteForge.Toolkit.Utils;
using System.Reflection;

namespace ByteForge.Toolkit.Configuration;
/// <summary>
/// Provides helper methods that leverage an <see cref="IParser"/> to parse textual configuration
/// values and populate object instances or property values using reflection.
/// This helper focuses on:
/// <list type="bullet">
/// <item>Resolving nullable types to their underlying non-nullable forms.</item>
/// <item>Creating default instances for properties via <c>DefaultValueHelper.ResolveDefaultValue</c>.</item>
/// <item>Parsing a string into a temporary object graph and copying writable public properties onto an existing target.</item>
/// </list>
/// </summary>
/// <remarks>
/// The parsing strategy intentionally performs a shallow copy of public writable instance properties
/// from a parsed temporary instance to the provided target instance, ignoring properties whose parsed value is <c>null</c>.
/// </remarks>
internal class ParsingHelper : IParsingHelper
{
    private readonly IParser parser;

    /// <summary>
    /// Gets a globally accessible default instance of <see cref="ParsingHelper"/> that uses <see cref="Parser.Default"/>.
    /// </summary>
    public static IParsingHelper Default { get; } = new ParsingHelper();

    /// <summary>
    /// Initializes a new instance of the <see cref="ParsingHelper"/> class.
    /// </summary>
    /// <param name="parser">
    /// The <see cref="IParser"/> implementation to delegate parsing operations to.
    /// If <c>null</c>, <see cref="Parser.Default"/> is used.
    /// </param>
    public ParsingHelper(IParser? parser = null)
    {
        this.parser = parser ?? Parser.Default;
    }

    /// <summary>
    /// Parses the provided string <paramref name="value"/> and populates the specified <paramref name="prop"/> on the given <paramref name="owner"/>.
    /// </summary>
    /// <param name="prop">The property metadata to populate.</param>
    /// <param name="owner">The object instance that owns the property.</param>
    /// <param name="value">The textual representation to parse. If null or whitespace, the property's default value is applied.</param>
    /// <remarks>
    /// This method delegates to the default <see cref="ParsingHelper"/> instance.
    /// </remarks>
    public static void ParseAndPopulateProperty(PropertyInfo prop, object owner, string value) =>
               Default.ParseAndPopulateProperty(prop, owner, value);

    /// <summary>
    /// Parses the provided string <paramref name="value"/> into an object of the specified <paramref name="type"/>
    /// and copies its public writable property values onto the supplied <paramref name="target"/> instance.
    /// </summary>
    /// <param name="type">The target type to parse into. Nullable wrappers are unwrapped.</param>
    /// <param name="target">The existing object instance to populate.</param>
    /// <param name="value">The textual representation to parse. If null or whitespace, no action is taken.</param>
    /// <remarks>
    /// This method delegates to the default <see cref="ParsingHelper"/> instance.
    /// </remarks>
    public static void ParseAndPopulateObject(Type type, object target, string value) =>
               Default.ParseAndPopulateObject(type, target, value);

    /// <summary>
    /// Parses a string and assigns the resulting object to the specified property on the provided owner instance.
    /// If the string is null or whitespace, a default value (resolved via <c>DefaultValueHelper</c>) is assigned.
    /// </summary>
    /// <param name="prop">The property to set.</param>
    /// <param name="owner">The instance whose property will be set.</param>
    /// <param name="value">The raw string to parse.</param>
    /// <remarks>
    /// A default object instance is created first, then populated using <see cref="IParsingHelper.ParseAndPopulateObject(Type, object, string)"/>.
    /// </remarks>
    void IParsingHelper.ParseAndPopulateProperty(PropertyInfo prop, object owner, string value)
    {
        var result = DefaultValueHelper.ResolveDefaultValue(prop);
        if (string.IsNullOrWhiteSpace(value))
        {
            prop.SetValue(owner, result);
            return;
        }

        var type = TypeHelper.ResolveType(prop);
        ((IParsingHelper)this).ParseAndPopulateObject(type, result, value);
        prop.SetValue(owner, result);
    }

    /// <summary>
    /// Parses the provided string into an object of the given <paramref name="type"/> and copies its non-null
    /// public writable instance property values onto the supplied <paramref name="target"/>.
    /// </summary>
    /// <param name="type">The type describing the shape of the object to parse. Nullable wrappers are unwrapped.</param>
    /// <param name="target">The existing object whose properties will be updated.</param>
    /// <param name="value">The textual representation to parse. If null or whitespace, the method returns immediately.</param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="type"/> cannot be resolved.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="target"/> is <c>null</c>.</exception>
    /// <remarks>
    /// Only properties that are both readable and writable are considered. Properties with parsed values of <c>null</c> are skipped.
    /// </remarks>
    void IParsingHelper.ParseAndPopulateObject(Type? type, object? target, string? value)
    {
        type = TypeHelper.ResolveType(type) ?? throw new ArgumentException("Type cannot be resolved.", nameof(type));
        target = target ?? throw new ArgumentNullException(nameof(target));
        if (string.IsNullOrWhiteSpace(value))
            return;

        var parsedObject = parser.Parse(type, value!);
        var subProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                    .Where(p => p.CanRead && p.CanWrite);

        foreach (var subProp in subProperties)
        {
            var subValue = subProp.GetValue(parsedObject);
            if (subValue == null)
                continue;
            subProp.SetValue(target, subValue);
        }
    }
}
