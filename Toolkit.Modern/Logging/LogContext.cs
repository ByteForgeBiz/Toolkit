using System;
using System.Collections.Generic;
using System.Threading;

namespace ByteForge.Toolkit.Logging
{
    /// <summary>
    /// Provides execution context for logging, allowing automatic correlation of log entries
    /// with specific operations or requests. Uses AsyncLocal to flow context across async/await boundaries.
    /// </summary>
    public static class LogContext
    {
        private static readonly AsyncLocal<string?> _pageIdentifier = new AsyncLocal<string?>();
        private static readonly AsyncLocal<LogRoutingContext?> _routingContext = new AsyncLocal<LogRoutingContext?>();

        /// <summary>
        /// Gets or sets the current page identifier for this execution context.
        /// This value automatically flows across async/await boundaries.
        /// </summary>
        /// <value>
        /// The page identifier string (e.g., "ABC1", "Xy3z"), or null if not set.
        /// </value>
        public static string? PageIdentifier
        {
            get => _pageIdentifier.Value;
            set => _pageIdentifier.Value = value;
        }

        /// <summary>
        /// Gets or sets the current routing context for this execution scope.
        /// </summary>
        public static LogRoutingContext? RoutingContext
        {
            get => _routingContext.Value;
            set => _routingContext.Value = value;
        }

        /// <summary>
        /// Sets the page identifier for the current execution context and returns a disposable scope.
        /// When the scope is disposed, the previous page identifier is restored.
        /// </summary>
        /// <param name="pageId">The page identifier to set.</param>
        /// <returns>A disposable scope that restores the previous page identifier when disposed.</returns>
        /// <example>
        /// <code>
        /// using (LogContext.SetPageIdentifier("ABC1"))
        /// {
        ///     Log.Info("This message will be prefixed with [ABC1]");
        ///     await DoWorkAsync(); // PageIdentifier flows through async operations
        ///     Log.Info("This too will have [ABC1]");
        /// } // Previous PageIdentifier is restored here
        /// </code>
        /// </example>
        public static IDisposable SetPageIdentifier(string pageId)
        {
            var previous = _pageIdentifier.Value;
            _pageIdentifier.Value = pageId;
            return new Scope(() => _pageIdentifier.Value = previous);
        }

        /// <summary>
        /// Applies scoped log routing for the current execution context.
        /// </summary>
        /// <param name="additionalLoggers">Additional loggers that should receive entries in this scope.</param>
        /// <param name="suppressedLoggerNames">Logger names that should not receive entries in this scope.</param>
        /// <returns>A disposable scope that restores the previous routing context.</returns>
        public static IDisposable BeginRoutingScope(IEnumerable<ILogger> additionalLoggers = null, IEnumerable<string> suppressedLoggerNames = null)
        {
            var previous = _routingContext.Value;
            var next = new LogRoutingContext(additionalLoggers, suppressedLoggerNames);
            _routingContext.Value = previous?.Merge(next) ?? next;
            return new Scope(() => _routingContext.Value = previous);
        }

        /// <summary>
        /// Clears the page identifier from the current execution context.
        /// </summary>
        public static void Clear()
        {
            _pageIdentifier.Value = null;
            _routingContext.Value = null;
        }

        /// <summary>
        /// Internal helper class that implements IDisposable to restore context on scope exit.
        /// </summary>
        private class Scope : IDisposable
        {
            private readonly Action _onDispose;
            private bool _disposed;

            public Scope(Action onDispose)
            {
                _onDispose = onDispose;
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _onDispose?.Invoke();
                    _disposed = true;
                }
            }
        }
    }
}
