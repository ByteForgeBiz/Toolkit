using System;
using System.Text.RegularExpressions;

namespace ByteForge.Toolkit;

/*
 *  _   _     _ 
 * | | | |_ _| |
 * | |_| | '_| |
 *  \___/|_| |_|
 *              
 */
/// <summary>
/// Provides methods for URL manipulation.
/// </summary>
public static class Url
{
    /// <summary>
    /// Regular expression for matching URLs.
    /// </summary>
    private static readonly Regex rxUrl = new Regex(@"^((http|ftp)s?://|/)", RegexOptions.IgnoreCase);

    /// <summary>
    /// Regular expression for matching incorrect URLs.
    /// </summary>
    private static readonly Regex rxUrlWrong = new Regex(@"^((http|ftp)s?:/)", RegexOptions.IgnoreCase);

    /// <summary>
    /// Combines two URL fragments into one.
    /// </summary>
    /// <param name="url1">The first URL.</param>
    /// <param name="url2">The second URL.</param>
    /// <returns>The combined URL.</returns>
    public static string Combine(string url1, string url2)
    {
        if (string.IsNullOrEmpty(url1) && string.IsNullOrEmpty(url2))
            return string.Empty;

        if (rxUrl.IsMatch(url2))
            return url2;

        // combine both parts and remove any duplicate slashes
        var result = $"{url1.TrimEnd('/')}/{url2.TrimStart('/')}".Replace('\\', '/');
        result = result.Replace("//", "/");

        return rxUrlWrong.Replace(result, "$1/"); 
    }

    /// <summary>
    /// Combines three URL fragments into one.
    /// </summary>
    /// <param name="url1">The first URL.</param>
    /// <param name="url2">The second URL.</param>
    /// <param name="url3">The third URL.</param>
    /// <returns>The combined URL.</returns>
    public static string Combine(string url1, string url2, string url3)
    {
        return Combine(Combine(url1, url2), url3);
    }

    /// <summary>
    /// Combines four URL fragemtss into one.
    /// </summary>
    /// <param name="url1">The first URL.</param>
    /// <param name="url2">The second URL.</param>
    /// <param name="url3">The third URL.</param>
    /// <param name="url4">The fourth URL.</param>
    /// <returns>The combined URL.</returns>
    public static string Combine(string url1, string url2, string url3, string url4)
    {
        return Combine(Combine(url1, url2), Combine(url3, url4));
    }

    /// <summary>
    /// Combines an array of URLs into one.
    /// </summary>
    /// <param name="urls">The array of URLs.</param>
    /// <returns>The combined URL.</returns>
    public static string Combine(params string[] urls)
    {
        if (urls == null || urls.Length == 0)
            return string.Empty;

        var result = urls[0];
        for (var i = 1; i < urls.Length; i++)
            result = Combine(result, urls[i]);

        return result;
    }

    /// <summary>
    /// Returns the domain of the specified URL.
    /// </summary>
    /// <param name="apiUrl">The URL.</param>
    /// <returns>The domain of the URL.</returns>
    public static string GetDomain(string apiUrl) => new Uri(apiUrl).Host;
}
