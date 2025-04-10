namespace ByteForge.Toolkit
{
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
        CompressedAndSplit,

        /// <summary>
        /// Processing failed.
        /// </summary>
        Failed
    }
}