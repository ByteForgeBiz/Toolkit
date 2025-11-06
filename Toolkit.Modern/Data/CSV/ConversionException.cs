namespace ByteForge.Toolkit;
/*
 *   ___                        _          ___                 _   _          
 *  / __|___ _ ___ _____ _ _ __(_)___ _ _ | __|_ ____ ___ _ __| |_(_)___ _ _  
 * | (__/ _ \ ' \ V / -_) '_(_-< / _ \ ' \| _|\ \ / _/ -_) '_ \  _| / _ \ ' \ 
 *  \___\___/_||_\_/\___|_| /__/_\___/_||_|___/_\_\__\___| .__/\__|_\___/_||_|
 *                                                       |_|                  
 */
/// <summary>
/// Represents errors that occur during conversion operations.
/// </summary>
public class ConversionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConversionException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ConversionException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConversionException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ConversionException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConversionException"/> class with a specified error message, column name, and value.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="columnName">The name of the column where the error occurred.</param>
    /// <param name="value">The value that caused the error.</param>
    public ConversionException(string message, string columnName, string value) : this(message)
    {
        Data.Add("ColumnName", columnName);
        Data.Add("Value", value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConversionException"/> class with a specified error message, column name, value, and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="columnName">The name of the column where the error occurred.</param>
    /// <param name="value">The value that caused the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ConversionException(string message, string columnName, string value, Exception innerException) : this(message, innerException)
    {
        Data.Add("ColumnName", columnName);
        Data.Add("Value", value);
    }
}
