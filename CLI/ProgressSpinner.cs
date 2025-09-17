using System;
using System.Threading;

namespace ByteForge.Toolkit
{
    /// <summary>
    /// Provides a self-contained progress spinner that uses ConsoleUtil's DrawProgressBar functionality.
    /// This class allows for easy use with 'using' statements to automatically start and stop
    /// progress spinners around long-running operations with unknown duration.
    /// </summary>
    /// <remarks>
    /// Similar to <see cref="ConsoleSpinner"/>, this class manages its own background thread and animation
    /// but leverages the existing <see cref="ConsoleUtil.DrawProgressBar"/> infrastructure with indefinite 
    /// progress (-1 percentage) to provide consistent visual feedback with the existing progress bar system.
    /// The spinner automatically starts when the object is created and stops when disposed.
    /// </remarks>
    /// <example>
    /// <code>
    /// using (new ProgressSpinner("Connecting to API..."))
    /// {
    ///     // Perform long-running operation
    ///     var result = await apiClient.FetchDataAsync();
    /// }
    /// // Spinner automatically stops here
    /// </code>
    /// </example>
    public class ProgressSpinner : IDisposable
    {
        #region Fields

        /// <summary>
        /// Global lock object to synchronize console access across threads.
        /// </summary>
        private static readonly object _globalConsoleLock = new object();

        /// <summary>
        /// The thread responsible for running the spinner animation.
        /// </summary>
        private readonly Thread _spinThread;

        /// <summary>
        /// Indicates whether the spinner is currently running.
        /// </summary>
        private volatile bool _isSpinning;

        /// <summary>
        /// The current message being displayed with the spinner.
        /// </summary>
        private volatile string _message;

        /// <summary>
        /// Indicates whether the spinner has been disposed.
        /// </summary>
        private volatile bool _disposed = false;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressSpinner"/> class and starts the spinner.
        /// </summary>
        /// <param name="message">The message to display with the spinner. Defaults to "Working...".</param>
        /// <remarks>
        /// The spinner will automatically start displaying when this constructor is called.
        /// If no console is available, the spinner will not display but will not cause errors.
        /// </remarks>
        public ProgressSpinner(string message = "Working...")
        {
            _message = message ?? "Working...";
            
            if (!ConsoleUtil.IsConsoleAvailable) return;

            _isSpinning = true;
            _spinThread = new Thread(SpinLoop) { IsBackground = true };
            _spinThread.Start();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the message displayed with the spinner.
        /// </summary>
        /// <value>The message to display. Setting this updates the spinner display immediately.</value>
        /// <remarks>
        /// Changing this property while the spinner is active will update the displayed message
        /// without stopping and restarting the animation.
        /// </remarks>
        public string Message
        {
            get { return _message; }
            set
            {
                if (_disposed) return;
                _message = value ?? "Working...";
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Updates the spinner message to show the current operation.
        /// </summary>
        /// <param name="message">The new message to display.</param>
        /// <remarks>
        /// This is a convenience method equivalent to setting the <see cref="Message"/> property.
        /// If the spinner has been disposed, this method has no effect.
        /// </remarks>
        public void UpdateMessage(string message)
        {
            Message = message;
        }

        /// <summary>
        /// Stops the progress spinner and clears the display.
        /// </summary>
        /// <remarks>
        /// This method is automatically called when the object is disposed.
        /// It's safe to call this method multiple times.
        /// </remarks>
        public void Stop()
        {
            if (_disposed) return;

            _isSpinning = false;
            
            if (_spinThread?.IsAlive == true)
                _spinThread.Join(1000);

            // Clear the progress bar line
            if (ConsoleUtil.IsConsoleAvailable)
            {
                lock (_globalConsoleLock)
                {
                    Console.Write(new string(' ', Console.WindowWidth - 1) + "\r");
                }
            }

            _disposed = true;
        }

        /// <summary>
        /// Disposes the ProgressSpinner and stops the animation.
        /// </summary>
        /// <remarks>
        /// This method implements the IDisposable pattern and automatically stops
        /// the spinner when called. It's safe to call multiple times.
        /// </remarks>
        public void Dispose()
        {
            Stop();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Background thread loop for continuous spinner animation.
        /// </summary>
        /// <remarks>
        /// This method runs in a background thread and continuously calls <see cref="ConsoleUtil.DrawProgressBar"/>
        /// with a negative percentage to create the animated spinner effect. The loop continues
        /// until <see cref="_isSpinning"/> is set to false.
        /// </remarks>
        private void SpinLoop()
        {
            while (_isSpinning && !_disposed)
            {
                if (!ConsoleUtil.IsConsoleAvailable)
                    break;

                lock (_globalConsoleLock)
                {
                    if (_isSpinning && !_disposed)
                        ConsoleUtil.DrawProgressBar(-1, _message);
                }

                Thread.Sleep(100);
            }
        }

        #endregion
    }
}