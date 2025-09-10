using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace ByteForge.Toolkit.Logging
{
    /*
     *   ___                        _ _       _                           
     *  / __|___ _ __  _ __  ___ __(_) |_ ___| |   ___  __ _ __ _ ___ _ _ 
     * | (__/ _ \ '  \| '_ \/ _ (_-< |  _/ -_) |__/ _ \/ _` / _` / -_) '_|
     *  \___\___/_|_|_| .__/\___/__/_|\__\___|____\___/\__, \__, \___|_|  
     *                |_|                              |___/|___/         
     */
    /// <summary>
    /// Composite logger that logs messages to multiple loggers.
    /// </summary>
    public class CompositeLogger : BaseLogger, IList<ILogger>, IDisposable
    {
        private readonly bool _continueOnError;
        private readonly int? _maxDegreeOfParallelism;
        private bool enableMultiThreading = false;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeLogger"/> class with the specified name.
        /// </summary>
        /// <param name="name">The name of the logger.</param>
        protected CompositeLogger(string name) : base(name) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeLogger"/> class with two loggers.
        /// </summary>
        /// <param name="logger1">The first logger.</param>
        /// <param name="logger2">The second logger.</param>
        public CompositeLogger(ILogger logger1, ILogger logger2) : this(new ILogger[] { logger1, logger2 }) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeLogger"/> class with three loggers.
        /// </summary>
        /// <param name="logger1">The first logger.</param>
        /// <param name="logger2">The second logger.</param>
        /// <param name="logger3">The third logger.</param>
        public CompositeLogger(ILogger logger1, ILogger logger2, ILogger logger3) : this(new ILogger[] { logger1, logger2, logger3 }) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeLogger"/> class with a variable number of loggers.
        /// </summary>
        /// <param name="loggers">Array of loggers to log messages to.</param>
        public CompositeLogger(params ILogger[] loggers) : this(loggers, true) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeLogger"/> class.
        /// </summary>
        /// <param name="loggers">Array of loggers to log messages to.</param>
        /// <param name="continueOnError">Indicates whether to continue logging if one logger fails.</param>
        /// <param name="maxDegreeOfParallelism">Maximum degree of parallelism for logging operations.</param>
        public CompositeLogger(ILogger[] loggers, bool continueOnError = true, int? maxDegreeOfParallelism = 2) : base("CompositeLogger")
        {
            Loggers.AddRange(loggers);
            _continueOnError = continueOnError;
            _maxDegreeOfParallelism = maxDegreeOfParallelism;
        }

        /// <summary>
        /// Gets the number of loggers in the composite logger.
        /// </summary>
        public int Count => Loggers.Count;

        /// <summary>
        /// Gets the list of loggers in the composite logger.
        /// </summary>
        protected List<ILogger> Loggers { get; } = new List<ILogger>();

        /// <summary>
        /// Gets a value indicating whether the current application is a web application.
        /// </summary>
        private bool IsWebApp => (HttpContext.Current != null);

        /// <summary>
        /// Gets or sets a value indicating whether multi-threading is enabled for logging operations.
        /// </summary>
        /// <remarks>
        /// Multi-threading is disabled if the current application is a web application.
        /// </remarks>
        public bool EnableMultiThreading
        {
            get => !IsWebApp && enableMultiThreading;
            set => enableMultiThreading = value;
        }

        /// <summary>
        /// Gets a value indicating whether the collection is read-only.
        /// </summary>
        public bool IsReadOnly => ((ICollection<ILogger>)Loggers).IsReadOnly;

        /// <summary>
        /// Gets or sets the logger at the specified index.
        /// </summary>
        /// <param name="index">The index of the logger.</param>
        /// <returns>The logger at the specified index.</returns>
        public ILogger this[int index] { get => ((IList<ILogger>)Loggers)[index]; set => ((IList<ILogger>)Loggers)[index] = value; }

        /// <summary>
        /// Clears all loggers from the composite logger.
        /// </summary>
        public void ClearLoggers() => Loggers.Clear();

        /// <summary>
        /// Adds a logger to the composite logger.
        /// </summary>
        /// <param name="logger">The logger to add.</param>
        public void AddLogger(ILogger logger) => Loggers.Add(logger);

        /// <summary>
        /// Removes a logger from the composite logger.
        /// </summary>
        /// <param name="logger">The logger to remove.</param>
        public void RemoveLogger(ILogger logger) => Loggers.Remove(logger);

        /// <summary>
        /// Removes a logger from the composite logger by name.
        /// </summary>
        /// <param name="name">The name of the logger to remove.</param>
        public void RemoveLogger(string name) => Loggers.RemoveAll(l => l.Name == name);

        /// <summary>
        /// Records a log entry by logging it to all configured loggers.
        /// </summary>
        /// <param name="entry">The log entry to record.</param>
        /// <exception cref="Exception">Thrown if logging fails and <see cref="_continueOnError"/> is set to false.</exception>
        protected internal override void RecordLogEntry(LogEntry entry)
        {
            var cc = CorrelationContext.New();
            entry.CorrelationId = cc.Id;

            if (EnableMultiThreading == false)
            {
                RecordLogEntrySynchronously(entry);
                return;
            }

            var tasks = Loggers.Select(logger => Task.Run(() =>
            {
                if (!(logger is BaseLogger log))
                    return;

                try
                {
                    log.RecordLogEntry(entry);
                }
                catch
                {
                    if (!_continueOnError)
                        throw;
                }
            })).ToArray();

            if (_maxDegreeOfParallelism.HasValue)
            {
                // Process tasks in batches if max parallelism is specified
                var chunk = _maxDegreeOfParallelism.Value;
                for (var i = 0; i < tasks.Length; i += chunk)
                    Task.Run(() => Task.WhenAll(tasks.Skip(i).Take(chunk)));
            }
            else
            {
                // Run all tasks in parallel
                Task.Run(() => Task.WhenAll(tasks));
            }
        }

        /// <summary>
        /// Records a log entry synchronously.
        /// </summary>
        /// <param name="entry">The log entry to record.</param>
        private void RecordLogEntrySynchronously(LogEntry entry)
        {
            Loggers.All(logger =>
            {
                if (!(logger is BaseLogger log))
                    return true;

                try
                {
                    log.RecordLogEntry(entry);
                }
                catch
                {
                    if (!_continueOnError)
                        throw;
                }
                return true;
            });
        }

        /// <summary>
        /// Gets the index of the specified logger.
        /// </summary>
        /// <param name="item">The logger to locate.</param>
        /// <returns>The index of the logger if found; otherwise, -1.</returns>
        public int IndexOf(ILogger item)
        {
            return ((IList<ILogger>)Loggers).IndexOf(item);
        }

        /// <summary>
        /// Inserts a logger at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which the logger should be inserted.</param>
        /// <param name="item">The logger to insert.</param>
        public void Insert(int index, ILogger item)
        {
            ((IList<ILogger>)Loggers).Insert(index, item);
        }

        /// <summary>
        /// Removes the logger at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the logger to remove.</param>
        public void RemoveAt(int index)
        {
            ((IList<ILogger>)Loggers).RemoveAt(index);
        }

        /// <summary>
        /// Adds a logger to the composite logger.
        /// </summary>
        /// <param name="item">The logger to add.</param>
        public void Add(ILogger item)
        {
            ((ICollection<ILogger>)Loggers).Add(item);
        }

        /// <summary>
        /// Clears all loggers from the composite logger.
        /// </summary>
        public void Clear()
        {
            ((ICollection<ILogger>)Loggers).Clear();
        }

        /// <summary>
        /// Determines whether the composite logger contains the specified logger.
        /// </summary>
        /// <param name="item">The logger to locate.</param>
        /// <returns>true if the logger is found; otherwise, false.</returns>
        public bool Contains(ILogger item)
        {
            return ((ICollection<ILogger>)Loggers).Contains(item);
        }

        /// <summary>
        /// Copies the elements of the composite logger to an array, starting at a particular array index.
        /// </summary>
        /// <param name="array">The one-dimensional array that is the destination of the elements copied from the composite logger.</param>
        /// <param name="arrayIndex">The zero-based index in the array at which copying begins.</param>
        public void CopyTo(ILogger[] array, int arrayIndex)
        {
            ((ICollection<ILogger>)Loggers).CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Removes the first occurrence of a specific logger from the composite logger.
        /// </summary>
        /// <param name="item">The logger to remove.</param>
        /// <returns>true if the logger was successfully removed; otherwise, false.</returns>
        public bool Remove(ILogger item)
        {
            return ((ICollection<ILogger>)Loggers).Remove(item);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the composite logger.
        /// </summary>
        /// <returns>An enumerator for the composite logger.</returns>
        public IEnumerator<ILogger> GetEnumerator()
        {
            return ((IEnumerable<ILogger>)Loggers).GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the composite logger.
        /// </summary>
        /// <returns>An enumerator for the composite logger.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)Loggers).GetEnumerator();
        }

        /// <summary>
        /// Releases the resources used by the current instance of the class.
        /// </summary>
        /// <param name="disposing">A value indicating whether to release both managed and unmanaged resources  (<see langword="true"/>) or only unmanaged resources (<see langword="false"/>).</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
                foreach (var logger in Loggers)
                    if (logger is IDisposable disposable)
                        disposable.Dispose();

            _disposed = true;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed)
                return;

            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}