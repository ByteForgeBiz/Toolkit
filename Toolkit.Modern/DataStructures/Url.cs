using System;
using System.Linq;
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

        var m = rxUrl.Match(url2);
        if (m.Success)
            return (m.Value == "/" ? "/" : "") + url2.Trim('/');

        // combine both parts and remove any duplicate slashes
        var result = $"{url1.TrimEnd('/')}/{url2.TrimStart('/')}".Replace('\\', '/');
        result = result.Replace("//", "/").Trim('/');

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

        
        return Combine(urls[0], urls.Skip(1).ToArray());
    }

    /// <summary>
    /// Combines a base URL with an array of URL fragments or parameters into a single URL.
    /// </summary>
    /// <param name="baseUrl">The base URL to which the fragments or parameters will be appended.</param>
    /// <param name="parameters">An array of URL fragments or parameters to append to the base URL.</param>
    /// <returns>The combined URL as a string. Returns an empty string if <paramref name="parameters"/> is null or empty.</returns>
    public static string Combine(string baseUrl, string[] parameters)
    {
        if (parameters == null || parameters.Length == 0)
            return string.Empty;

        var result = baseUrl;
        for (var i = 0; i < parameters.Length; i++)
            result = Combine(result, parameters[i]);

        return result;
    }

    /// <summary>
    /// Returns the domain of the specified URL.
    /// </summary>
    /// <param name="apiUrl">The URL.</param>
    /// <returns>The domain of the URL.</returns>
    public static string GetDomain(string apiUrl) => new Uri(apiUrl).Host;
}
