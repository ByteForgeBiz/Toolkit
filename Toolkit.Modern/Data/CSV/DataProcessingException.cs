using System;
using System.Runtime.Serialization;

namespace ByteForge.Toolkit
{
    /*
     *  ___       _        ___                       _           ___                 _   _          
     * |   \ __ _| |_ __ _| _ \_ _ ___  __ ___ _____(_)_ _  __ _| __|_ ____ ___ _ __| |_(_)___ _ _  
     * | |) / _` |  _/ _` |  _/ '_/ _ \/ _/ -_)_-<_-< | ' \/ _` | _|\ \ / _/ -_) '_ \  _| / _ \ ' \ 
     * |___/\__,_|\__\__,_|_| |_| \___/\__\___/__/__/_|_||_\__, |___/_\_\__\___| .__/\__|_\___/_||_|
     *                                                     |___/               |_|                  
     */
    /// <summary>
    /// Represents errors that occur during data processing.
    /// </summary>
    [Serializable]
    internal class DataProcessingException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataProcessingException"/> class.
        /// </summary>
        public DataProcessingException() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataProcessingException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public DataProcessingException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataProcessingException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
        public DataProcessingException(string message, Exception innerException) : base(message, innerException) { }
    }
}