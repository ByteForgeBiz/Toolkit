using System.Text.RegularExpressions;

namespace ByteForge.Toolkit.Utils;
/*
 *  _____               _      _       ___                                
 * |_   _|__ _ __  _ __| |__ _| |_ ___| _ \_ _ ___  __ ___ ______ ___ _ _ 
 *   | |/ -_) '  \| '_ \ / _` |  _/ -_)  _/ '_/ _ \/ _/ -_)_-<_-</ _ \ '_|
 *   |_|\___|_|_|_| .__/_\__,_|\__\___|_| |_| \___/\__\___/__/__/\___/_|  
 *                |_|                                                     
 */
/// <summary>
/// Processes a dictionary of key-value pairs and replaces placeholders in text with corresponding values.
/// </summary>
public sealed class TemplateProcessor
{
    private static readonly Regex KeyValidator = new Regex(@"[<>]", RegexOptions.Compiled);
    private static readonly Regex PlaceholderFinder = new Regex(@"<(?<key>[^<>]+?)>", RegexOptions.Compiled);

    private readonly Dictionary<string, string> _dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets a value indicating whether escape sequences should be used in the output.
    /// </summary>
    /// <remarks>
    /// If set to true, the output will replace escape sequences with their corresponding characters.
    /// </remarks>
    public bool UseEscapeSequences { get; set; } = false;

    /// <summary>
    /// Adds a key-value pair to the dictionary.
    /// </summary>
    /// <param name="key">The key to add.</param>
    /// <param name="value">The value to associate with the key.</param>
    /// <exception cref="ArgumentNullException">Thrown when the key is null or empty.</exception>
    /// <exception cref="ArgumentException">Thrown when the key contains invalid characters.</exception>
    public void Add(string key, string value)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));

        if (KeyValidator.IsMatch(key))
            throw new ArgumentException($"Key cannot contain < or >: '{key}'");

        _dictionary[key] = value ?? string.Empty;
    }

    /// <summary>
    /// Removes a key-value pair from the dictionary.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    /// <exception cref="ArgumentNullException">Thrown when the key is null or empty.</exception>
    public void Remove(string key)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));

        _dictionary.Remove(key);
    }

    /// <summary>
    /// Gets the number of key-value pairs in the dictionary.
    /// </summary>
    public int Count => _dictionary.Count;

    /// <summary>
    /// Clears all key-value pairs from the dictionary.
    /// </summary>
    public void Clear() => _dictionary.Clear();

    /// <summary>
    /// Processes the input text by replacing placeholders with corresponding values from the dictionary.
    /// </summary>
    /// <param name="text">The input text containing placeholders.</param>
    /// <returns>The processed text with placeholders replaced by dictionary values.</returns>
    public string Process(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;

        var result = PlaceholderFinder.Replace(text, match =>
        {
            var key = match.Groups["key"].Value;
            return _dictionary.TryGetValue(key, out var value) ? value : match.Value;
        });

        if (UseEscapeSequences)
            result = Regex.Unescape(result);

        return result;
    }
}
