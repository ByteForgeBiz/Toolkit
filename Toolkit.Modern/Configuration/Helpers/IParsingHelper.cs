using System;
using System.Reflection;

namespace ByteForge.Toolkit;
/// <summary>
/// Defines parsing operations that transform a textual representation into object instances or
/// populate existing objects / properties using reflection.
/// </summary>
/// <remarks>
/// Implementations are expected to:
/// <list type="bullet">
/// <item>Support resolving and handling nullable types where appropriate.</item>
/// <item>Populate only writable, publicly accessible instance members.</item>
/// <item>Perform defensive checks for <c>null</c> arguments and empty input values.</item>
/// </list>
/// The contract does not prescribe deep vs. shallow population semantics; that behavior is left
/// to the concrete implementation (e.g., <see cref="ParsingHelper"/> performs a shallow property copy).
/// </remarks>
internal interface IParsingHelper
{
    /// <summary>
    /// Parses the supplied textual <paramref name="value"/> into an instance described by <paramref name="type"/>
    /// (or a compatible intermediary representation) and applies the resulting data onto the existing
    /// <paramref name="target"/> object.
    /// </summary>
    /// <param name="type">
    /// The runtime <see cref="Type"/> describing the shape expected from the parsed value.
    /// Implementations may unwrap nullable types (e.g., <c>Nullable&lt;T&gt;</c>).
    /// </param>
    /// <param name="target">
    /// The existing object to populate. Must not be <c>null</c>. Implementations typically update
    /// writable public instance properties only.
    /// </param>
    /// <param name="value">
    /// The textual representation to parse. Implementations may no-op if the value is <c>null</c>,
    /// empty, or whitespace.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="target"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Implementations may throw if <paramref name="type"/> is invalid or unsupported.</exception>
    void ParseAndPopulateObject(Type? type, object? target, string? value);

    /// <summary>
    /// Parses the supplied textual <paramref name="value"/> and assigns (or populates) the specified
    /// <paramref name="prop"/> on the given <paramref name="owner"/> instance.
    /// </summary>
    /// <param name="prop">
    /// The target property metadata. Must represent a writable property; implementations may
    /// validate accessibility or writability.
    /// </param>
    /// <param name="owner">
    /// The object instance that owns the property to be updated. Must not be <c>null</c>.
    /// </param>
    /// <param name="value">
    /// The textual representation to parse. Implementations may substitute a default value when
    /// the input is <c>null</c> or whitespace.
    /// </param>
    /// <exception cref="ArgumentNullException">Implementations may throw if <paramref name="prop"/> or <paramref name="owner"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Implementations may throw if the property is not writable or otherwise unsupported.</exception>
    void ParseAndPopulateProperty(PropertyInfo prop, object owner, string value);
}
