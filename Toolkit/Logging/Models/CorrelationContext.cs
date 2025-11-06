namespace ByteForge.Toolkit.Logging
{
    /*
     *   ___                 _      _   _          ___         _           _   
     *  / __|___ _ _ _ _ ___| |__ _| |_(_)___ _ _ / __|___ _ _| |_ _____ _| |_ 
     * | (__/ _ \ '_| '_/ -_) / _` |  _| / _ \ ' \ (__/ _ \ ' \  _/ -_) \ /  _|
     *  \___\___/_| |_| \___|_\__,_|\__|_\___/_||_\___\___/_||_\__\___/_\_\\__|
     *                                                                         
     */
    /// <summary>
    /// Provides a context for correlation IDs to track and correlate logs across different components.
    /// </summary>
    public class CorrelationContext
    {
        static int _currentId = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="CorrelationContext"/> class with the specified ID.
        /// </summary>
        /// <param name="id">The correlation ID. If null, a new GUID will be generated.</param>
        public CorrelationContext(string id)
        {
            Id = id ?? $"{(_currentId = ((_currentId + 1) & 0xffff)):x4}";
        }

        /// <summary>
        /// Gets the correlation ID.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Creates a new instance of the <see cref="CorrelationContext"/> class with a new correlation ID.
        /// </summary>
        /// <returns>A new <see cref="CorrelationContext"/> instance.</returns>
        public static CorrelationContext New() => new CorrelationContext(null);
    }
}