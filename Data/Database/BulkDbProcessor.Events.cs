using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ByteForge.Toolkit
{
    public partial class BulkDbProcessor<T>
    {

        /// <summary>
        /// Occurs when the process starts.
        /// </summary>
        public event MessageEventHandler Started;

        /// <summary>
        /// Occurs to report the progress of the process.
        /// </summary>
        public event ProgressEventHandler Progress;

        /// <summary>
        /// Occurs when a progress message is available.
        /// </summary>
        public event MessageEventHandler Message;

        /// <summary>
        /// Occurs when a warning is raised during the operation of the process.
        /// </summary>
        public event MessageEventHandler Warning;

        /// <summary>
        /// Occurs when an error happens during the process.
        /// </summary>
        public event ErrorEventHandler Error;

        /// <summary>
        /// Occurs when the process ends.
        /// </summary>
        public event EndProcessEventHandler Finished;

        /// <summary>
        /// Raises the <see cref="Started"/> event.
        /// </summary>
        /// <param name="message">Optional message (defaults to "Process started.").</param>
        protected virtual void OnStarted(string message = "Process started.")
        {
            Log.Info(message);
            Started?.Invoke(this, new MessageEventArgs(message));
        }

        /// <summary>
        /// Raises the <see cref="Progress"/> event.
        /// </summary>
        /// <param name="progress">Progress percentage (0-100). Values are clamped to this range.</param>
        protected virtual void OnProgress(double progress) => OnProgress(progress, null);

        /// <summary>
        /// Raises the <see cref="Progress"/> event with an associated message.
        /// </summary>
        /// <param name="progress">Progress percentage (0-100). Values are clamped to this range.</param>
        /// <param name="message">A message describing the progress.</param>
        protected virtual void OnProgress(double progress, string message)
        {
            progress = double.IsNaN(progress) ? 0 :
                       progress < 0 ? 0 :
                       progress > 100 ? 100 : progress;

            Log.Trace($"Progress: {progress}%{(string.IsNullOrEmpty(message) ? "" : " - " + message)}");
            Progress?.Invoke(this, new ProgressEventArgs(progress, message));
        }

        /// <summary>
        /// Raises the <see cref="Message"/> event.
        /// </summary>
        /// <param name="message">Informational message.</param>
        protected virtual void OnMessage(string message)
        {
            Log.Info(message);
            if (string.IsNullOrEmpty(message)) return;
            Message?.Invoke(this, new MessageEventArgs(message, MessageKind.Info));
        }

        /// <summary>
        /// Invokes the <see cref="Message"/> event with a debug message.
        /// </summary>
        /// <param name="message">The debug message to be sent. Cannot be null or empty.</param>
        protected virtual void OnDebug(string message)
        {
            Log.Verbose(message);
            if (string.IsNullOrEmpty(message)) return;
            Message?.Invoke(this, new MessageEventArgs(message, MessageKind.Debug));
        }

        /// <summary>
        /// Raises the <see cref="Warning"/> event.
        /// </summary>
        /// <param name="message">Warning message.</param>
        protected virtual void OnWarning(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            Log.Warning(message);
            Warning?.Invoke(this, new MessageEventArgs(message, MessageKind.Warning));
        }

        /// <summary>
        /// Raises the <see cref="Error"/> event with an exception.
        /// </summary>
        /// <param name="message">Error message.</param>
        /// <param name="ex">Associated exception.</param>
        protected virtual void OnError(string message, Exception ex)
        {
            Log.Error(message, ex);
            Error?.Invoke(this, new ErrorEventArgs(string.IsNullOrEmpty(message) ? ex?.Message ?? "An error occurred." : message, ex));
        }

        /// <summary>
        /// Raises the <see cref="Finished"/> event.
        /// </summary>
        /// <param name="aborted">Indicates whether the process was aborted.</param>
        /// <param name="message">Optional completion message (defaults to "Process finished.").</param>
        protected virtual void OnFinished(bool aborted = false, string message = null)
        {
            if (aborted)
                Log.Warning(string.IsNullOrWhiteSpace(message) ? "Process aborted." : message);
            else
                Log.Info(string.IsNullOrWhiteSpace(message) ? "Process finished." : message);

            var handler = Finished;
            if (handler != null)
            {
                if (aborted && string.IsNullOrWhiteSpace(message))
                    handler(this, new OperationFinishedEventArgs(true));
                else if (!aborted && !string.IsNullOrWhiteSpace(message))
                    handler(this, new OperationFinishedEventArgs(message));
                else if (!aborted && string.IsNullOrWhiteSpace(message))
                    handler(this, new OperationFinishedEventArgs("Process finished."));
                else // aborted with custom message
                    handler(this, new OperationFinishedEventArgs(true, message));
            }
        }
    }

    /// <summary>
    /// Represents the method that will handle a message event.
    /// </summary>
    /// <param name="source">The source of the event.</param>
    /// <param name="e">A <see cref="MessageEventArgs"/> that contains the event data.</param>
    public delegate void MessageEventHandler(object source, MessageEventArgs e);

    /// <summary>
    /// Represents the method that will handle the Progress event.
    /// </summary>
    /// <param name="source">The source of the event.</param>
    /// <param name="e">A <see cref="ProgressEventArgs"/> that contains the event data.</param>
    public delegate void ProgressEventHandler(object source, ProgressEventArgs e);

    /// <summary>
    /// Represents the method that will handle the Error event.
    /// </summary>
    /// <param name="source">The source of the event.</param>
    /// <param name="e">An <see cref="ErrorEventArgs"/> that contains the event data.</param>
    public delegate void ErrorEventHandler(object source, ErrorEventArgs e);

    /// <summary>
    /// Represents the method that will handle the EndProcess event.
    /// </summary>
    /// <param name="source">The source of the event.</param>
    /// <param name="e">An <see cref="OperationFinishedEventArgs"/> that contains the event data.</param>
    public delegate void EndProcessEventHandler(object source, OperationFinishedEventArgs e);

    /// <summary>
    /// Provides data for a message event, including the message text and its kind.
    /// </summary>
    public class MessageEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageEventArgs"/> class with an informational message.
        /// </summary>
        /// <param name="message">An informative message to the user.</param>
        internal MessageEventArgs(string message) : this(message, MessageKind.Info) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageEventArgs"/> class with a specified message and kind.
        /// </summary>
        /// <param name="message">An informative message to the user.</param>
        /// <param name="kind">The kind of message (Info, Warning, Error, EndProcess, Debug, Verbose).</param>
        internal MessageEventArgs(string message, MessageKind kind)
        {
            Message = message;
            Kind = kind;
        }

        /// <summary>
        /// Gets the informative message for the event.
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        /// Gets the kind of the message (Info, Warning, Error, EndProcess, Debug, Verbose).
        /// </summary>
        public MessageKind Kind { get; }
    }

    /// <summary>
    /// Specifies the kind of message for <see cref="MessageEventArgs"/>.
    /// </summary>
    public enum MessageKind
    {
        /// <summary>
        /// An informational message.
        /// </summary>
        Info,
        /// <summary>
        /// A warning message.
        /// </summary>
        Warning,
        /// <summary>
        /// An error message.
        /// </summary>
        Error,
        /// <summary>
        /// An end of process message.
        /// </summary>
        EndProcess,
        /// <summary>
        /// A debug message.
        /// </summary>
        Debug,
        /// <summary>
        /// A verbose message.
        /// </summary>
        Verbose
    }

    /// <summary>
    /// Provides data for the end process event.
    /// </summary>
    public class OperationFinishedEventArgs : MessageEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OperationFinishedEventArgs"/> class.
        /// </summary>
        /// <param name="aborted">A value indicating whether the process was aborted.</param>
        internal OperationFinishedEventArgs(bool aborted) : this(aborted, "Process aborted.") { }

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationFinishedEventArgs"/> class with the specified message.
        /// </summary>
        /// <param name="message">A message describing the end of the process.</param>
        internal OperationFinishedEventArgs(string message) : this(false, message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationFinishedEventArgs"/> class with the specified aborted status and message.
        /// </summary>
        /// <param name="aborted">A value indicating whether the process was aborted.</param>
        /// <param name="message">A message describing the end of the process.</param>
        internal OperationFinishedEventArgs(bool aborted, string message) : base(message, MessageKind.EndProcess)
        {
            Aborted = aborted;
        }

        /// <summary>
        /// Gets a value indicating whether the process was aborted.
        /// </summary>
        public bool Aborted { get; private set; }
    }
    /// <summary>
    /// Provides data for the Progress event.
    /// </summary>
    public class ProgressEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressEventArgs"/> class with the specified progress.
        /// </summary>
        /// <param name="progress">The percentage of the process completed.</param>
        internal ProgressEventArgs(double progress)
        {
            Progress = progress;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressEventArgs"/> class with the specified progress and message.
        /// </summary>
        /// <param name="progress">The percentage of the process completed.</param>
        /// <param name="message">A message describing the progress.</param>
        internal ProgressEventArgs(double progress, string message) : this(progress)
        {
            Message = message;
        }

        /// <summary>
        /// Gets or sets the percentage of the process completed.
        /// </summary>
        public double Progress { get; private set; }

        /// <summary>
        /// Gets or sets the message describing the progress.
        /// </summary>
        public string Message { get; private set; }
    }

    /// <summary>
    /// Provides data for the Error event, including the error message and the associated exception.
    /// </summary>
    public class ErrorEventArgs : MessageEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorEventArgs"/> class with the specified error message.
        /// </summary>
        /// <param name="message">The error message.</param>
        internal ErrorEventArgs(string message) : base(message, MessageKind.Error) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorEventArgs"/> class with the specified error message and exception.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="ex">The exception that caused the error.</param>
        internal ErrorEventArgs(string message, Exception ex) : this(message) => Exception = ex;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorEventArgs"/> class with the specified exception.
        /// The error message is taken from the exception's <see cref="Exception.Message"/> property.
        /// </summary>
        /// <param name="ex">The exception that caused the error.</param>
        internal ErrorEventArgs(Exception ex) : this(ex.Message, ex) { }

        /// <summary>
        /// Gets the exception that caused the error.
        /// </summary>
        public Exception Exception { get; }
    }
}
