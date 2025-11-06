namespace ByteForge.Toolkit;
/// <summary>
/// Defines methods for parsing string values into various data types.
/// </summary>
public interface IParser
{
    /// <summary>
    /// Determines whether the parser can handle the specified type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns><see langword="true"/> if the parser can handle the specified type; otherwise, <see langword="false"/>.</returns>
    bool IsKnownType(Type type);

    /// <summary>
    /// Registers a custom type with its corresponding parser and stringifier functions.
    /// </summary>
    /// <param name="type">The type to register.</param>
    /// <param name="parser">A function that converts a string to the specified type.</param>
    /// <param name="stringfier">A function that converts an object of the specified type to a string.</param>
    void RegisterType(Type type, Func<string, object> parser, Func<object, string> stringfier);

    /// <summary>
    /// Parses a string value into an object of the specified type.
    /// </summary>
    /// <param name="type">The target type to parse the value into.</param>
    /// <param name="value">The string value to parse.</param>
    /// <returns>The parsed object.</returns>
    /// <exception cref="ArgumentException">Thrown when the parser cannot handle the specified type.</exception>
    /// <exception cref="FormatException">Thrown when the string value is not in a correct format for the target type.</exception>
    object? Parse(Type? type, string value);

    /// <summary>
    /// Attempts to parse a string value into an object of the specified type.
    /// </summary>
    /// <param name="type">The target type to parse the value into.</param>
    /// <param name="value">The string value to parse.</param>
    /// <param name="result">When this method returns, contains the parsed object if parsing succeeded, or the default value if parsing failed.</param>
    /// <returns><see langword="true"/> if the value was successfully parsed; otherwise, <see langword="false"/>.</returns>
    bool TryParse(Type? type, string value, out object? result);

    /// <summary>
    /// Parses a string value into an object of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The target type to parse the value into.</typeparam>
    /// <param name="value">The string value to parse.</param>
    /// <returns>The parsed object of type <typeparamref name="T"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when the parser cannot handle the specified type.</exception>
    /// <exception cref="FormatException">Thrown when the string value is not in a correct format for the target type.</exception>
    T? Parse<T>(string value);

    /// <summary>
    /// Attempts to parse a string value into an object of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The target type to parse the value into.</typeparam>
    /// <param name="value">The string value to parse.</param>
    /// <param name="result">When this method returns, contains the parsed object if parsing succeeded, or the default value if parsing failed.</param>
    /// <returns><see langword="true"/> if the value was successfully parsed; otherwise, <see langword="false"/>.</returns>
    bool TryParse<T>(string value, out T? result);

    /// <summary>
    /// Converts an object to its string representation based on the specified type.
    /// </summary>
    /// <param name="type">The type of the object.</param>
    /// <param name="value">The object to convert to a string.</param>
    /// <returns>The string representation of the object.</returns>
    string Stringify(Type? type, object? value);

    /// <summary>
    /// Converts an object of type <typeparamref name="T"/> to its string representation.
    /// </summary>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <param name="value">The object to convert to a string.</param>
    /// <returns>The string representation of the object.</returns>
    string Stringify<T>(object? value);
}
