using System;
using System.Runtime.Serialization;

namespace ByteForge.Toolkit
{
    /*
     *  ___                       _                               _      __  __ _               _      _    ___                 _   _          
     * | _ \__ _ _ _ __ _ _ __   /_\  _ _ __ _ _  _ _ __  ___ _ _| |_ __|  \/  (_)____ __  __ _| |_ __| |_ | __|_ ____ ___ _ __| |_(_)___ _ _  
     * |  _/ _` | '_/ _` | '  \ / _ \| '_/ _` | || | '  \/ -_) ' \  _(_-< |\/| | (_-< '  \/ _` |  _/ _| ' \| _|\ \ / _/ -_) '_ \  _| / _ \ ' \ 
     * |_| \__,_|_| \__,_|_|_|_/_/ \_\_| \__, |\_,_|_|_|_\___|_||_\__/__/_|  |_|_/__/_|_|_\__,_|\__\__|_||_|___/_\_\__\___| .__/\__|_\___/_||_|
     *                                   |___/                                                                            |_|                  
     */
    /// <summary>
    /// The exception that is thrown when there is a mismatch between the parameters and arguments.
    /// </summary>
    [Serializable]
    public class ParamArgumentsMismatchException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ParamArgumentsMismatchException"/> class.
        /// </summary>
        public ParamArgumentsMismatchException() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParamArgumentsMismatchException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ParamArgumentsMismatchException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParamArgumentsMismatchException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
        public ParamArgumentsMismatchException(string message, Exception innerException) : base(message, innerException) { }
    }
}