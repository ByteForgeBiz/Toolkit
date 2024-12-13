using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ByteForge.Toolkit;

/*
 *  _   _ _   _ _    
 * | | | | |_(_) |___
 * | |_| |  _| | (_-<
 *  \___/ \__|_|_/__/
 *                   
 */
/// <summary>
/// Contains utility methods.
/// </summary>
public static class Utils
{

    /// <summary>
    /// Represents the carriage return and line feed character array.
    /// </summary>
    public static readonly char[] arrCRLF = ['\r', '\n'];

    /// <summary>
    /// Returns null if the provided string is null or empty; otherwise, returns the string itself.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <returns>Null if the string is null or empty; otherwise, the string itself.</returns>
    public static string NullIfEmpty(string value)
    {
        return string.IsNullOrEmpty(value) ? null : value;
    }

    /// <summary>
    /// Runs an asynchronous function synchronously.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="asyncFunc">The asynchronous function to run.</param>
    /// <returns>The result of the asynchronous function.</returns>
    public static T RunSync<T>(Func<CancellationToken, Task<T>> asyncFunc)
    {
        var task = Task.Run(() => asyncFunc(CancellationToken.None));
        task.Wait();
        return task.Result;
    }
}
