using System.Text.RegularExpressions;

namespace ByteForge.Toolkit.Utilities;
/// <summary>
/// Utility methods for HTML and CSS processing.
/// </summary>
public static class HtmlUtil
{
    /// <summary>
    /// Minifies a CSS string by removing comments, whitespace, and line breaks.
    /// </summary>
    /// <param name="css">The CSS string to minify.</param>
    /// <returns>The minified CSS string, or <see langword="null"/> if input is <see langword="null"/>.</returns>
    public static string MinifyCSS(string css)
    {
        if (string.IsNullOrWhiteSpace(css))
            return "";

        // Remove comments
        var minified = Regex.Replace(css, @"/\*[^*]*\*+([^/*][^*]*\*+)*/", string.Empty);
        // Remove whitespace around symbols
        minified = Regex.Replace(minified, @"\s*([{}:;,])\s*", "$1");
        // Remove unnecessary semicolons
        minified = Regex.Replace(minified, @";}", "}");
        // Collapse multiple spaces
        minified = Regex.Replace(minified, @"\s+", " ");
        // Trim
        return minified.Trim();
    }
}
