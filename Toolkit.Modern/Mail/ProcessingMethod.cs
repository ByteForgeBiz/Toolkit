namespace ByteForge.Toolkit
{
    /*
     *  ___                       _           __  __     _   _            _ 
     * | _ \_ _ ___  __ ___ _____(_)_ _  __ _|  \/  |___| |_| |_  ___  __| |
     * |  _/ '_/ _ \/ _/ -_)_-<_-< | ' \/ _` | |\/| / -_)  _| ' \/ _ \/ _` |
     * |_| |_| \___/\__\___/__/__/_|_||_\__, |_|  |_\___|\__|_||_\___/\__,_|
     *                                  |___/                               
     */
    /// <summary>
    /// Method used to process attachments.
    /// </summary>
    public enum ProcessingMethod
    {
        /// <summary>
        /// No processing method used.
        /// </summary>
        None,

        /// <summary>
        /// Files were attached directly.
        /// </summary>
        DirectAttachment,

        /// <summary>
        /// Files were compressed into a single archive.
        /// </summary>
        Compressed,

        /// <summary>
        /// Files were compressed and split into multiple archives.
        /// </summary>
        MultiPart,

        /// <summary>
        /// Processing failed.
        /// </summary>
        Failed
    }
}