using System;
using System.Collections.Generic;
using System.Linq;

namespace ByteForge.Toolkit.Logging
{
    /// <summary>
    /// Represents a scoped routing snapshot for a log entry.
    /// </summary>
    public sealed class LogRoutingContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LogRoutingContext"/> class.
        /// </summary>
        /// <param name="additionalLoggers">Additional loggers that should receive the entry.</param>
        /// <param name="suppressedLoggerNames">Logger names that should not receive the entry.</param>
        public LogRoutingContext(IEnumerable<ILogger> additionalLoggers = null, IEnumerable<string> suppressedLoggerNames = null)
        {
            AdditionalLoggers = (additionalLoggers ?? Array.Empty<ILogger>())
                .Where(logger => logger != null)
                .ToArray();

            SuppressedLoggerNames = new HashSet<string>(
                (suppressedLoggerNames ?? Array.Empty<string>())
                    .Where(name => string.IsNullOrWhiteSpace(name) == false),
                StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets the additional loggers that should receive the entry.
        /// </summary>
        public IReadOnlyCollection<ILogger> AdditionalLoggers { get; }

        /// <summary>
        /// Gets the logger names that should not receive the entry.
        /// </summary>
        public IReadOnlyCollection<string> SuppressedLoggerNames { get; }

        /// <summary>
        /// Merges this routing snapshot with another snapshot.
        /// </summary>
        /// <param name="other">The other snapshot.</param>
        /// <returns>A merged routing snapshot.</returns>
        public LogRoutingContext Merge(LogRoutingContext other)
        {
            if (other == null)
                return this;

            var loggerComparer = ReferenceEqualityComparer<ILogger>.Instance;

            return new LogRoutingContext(
                AdditionalLoggers.Concat(other.AdditionalLoggers).Distinct(loggerComparer),
                SuppressedLoggerNames.Concat(other.SuppressedLoggerNames));
        }

        /// <summary>
        /// Determines whether the specified logger should be suppressed.
        /// </summary>
        /// <param name="logger">The logger to evaluate.</param>
        /// <returns><see langword="true"/> when the logger should be suppressed; otherwise, <see langword="false"/>.</returns>
        public bool IsSuppressed(ILogger logger)
        {
            if (logger == null || string.IsNullOrWhiteSpace(logger.Name))
                return false;

            return SuppressedLoggerNames.Contains(logger.Name);
        }

        /// <summary>
        /// Compares reference types by object identity.
        /// </summary>
        /// <typeparam name="T">The reference type to compare.</typeparam>
        private sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T>
            where T : class
        {
            /// <summary>
            /// Gets the shared comparer instance.
            /// </summary>
            public static readonly ReferenceEqualityComparer<T> Instance = new ReferenceEqualityComparer<T>();

            /// <summary>
            /// Prevents direct construction of the comparer.
            /// </summary>
            private ReferenceEqualityComparer()
            {
            }

            /// <summary>
            /// Determines whether two references point to the same object.
            /// </summary>
            /// <param name="x">The first object reference.</param>
            /// <param name="y">The second object reference.</param>
            /// <returns><see langword="true"/> when both references point to the same object; otherwise, <see langword="false"/>.</returns>
            public bool Equals(T? x, T? y)
            {
                return ReferenceEquals(x, y);
            }

            /// <summary>
            /// Gets a hash code based on object identity.
            /// </summary>
            /// <param name="obj">The object reference.</param>
            /// <returns>The identity-based hash code for the object.</returns>
            public int GetHashCode(T obj)
            {
                return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
            }
        }
    }
}
