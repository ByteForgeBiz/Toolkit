using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ByteForge.Toolkit
{
    /*
     *    _  _   _           _                  _   ___                       ___             _ _   
     *   /_\| |_| |_ __ _ __| |_  _ __  ___ _ _| |_| _ \_ _ ___  __ ___ _____| _ \___ ____  _| | |_ 
     *  / _ \  _|  _/ _` / _| ' \| '  \/ -_) ' \  _|  _/ '_/ _ \/ _/ -_)_-<_-<   / -_)_-< || | |  _|
     * /_/ \_\__|\__\__,_\__|_||_|_|_|_\___|_||_\__|_| |_| \___/\__\___/__/__/_|_\___/__/\_,_|_|\__|
     *                                                                                              
     */
    /// <summary>
    /// Encapsulates the complete result of an email attachment processing operation.
    /// </summary>
    /// <remarks>
    /// When files are processed by the <see cref="EmailAttachmentHandler"/>, this class
    /// provides detailed information about what happened during processing, including:
    /// <list type="bullet">
    ///   <item>The method used to process attachments (direct, compressed, split)</item>
    ///   <item>Any temporary files created that may need cleanup</item>
    ///   <item>Files that were skipped and why</item>
    ///   <item>How files were distributed across multiple parts (if split)</item>
    ///   <item>Any errors that occurred during processing</item>
    /// </list>
    /// <para>
    /// This object is typically returned from attachment processing methods and should be
    /// inspected to verify processing was successful before proceeding with email sending.
    /// </para>
    /// <example>
    /// <code>
    /// var handler = new EmailAttachmentHandler();
    /// var result = handler.ProcessAttachments(files, maxSizeBytes);
    /// 
    /// if (result.Success)
    /// {
    ///     // Use result._tempFilesCreated for email attachments
    ///     // Check result.ProcessingMethod to determine how to construct email subject/body
    /// }
    /// else
    /// {
    ///     // Process failed, check result.Error for details
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    public class AttachmentProcessResult
    {
        /// <summary>
        /// Gets or sets the processing method used.
        /// </summary>
        /// <remarks>
        /// Indicates how the attachments were handled - whether they were attached directly,
        /// compressed into a single archive, split into multiple parts, or if processing failed.
        /// This can be used to determine how to structure the email subject and body text.
        /// </remarks>
        public ProcessingMethod ProcessingMethod { get; set; } = ProcessingMethod.None;

        /// <summary>
        /// Gets or sets the list of files that were skipped during processing.
        /// </summary>
        /// <remarks>
        /// If any files could not be processed (e.g., due to access restrictions or
        /// file corruption), they will be included here along with the reason they were skipped.
        /// <para>
        /// This information can be useful for reporting problems to users or for retry logic.
        /// </para>
        /// </remarks>
        public List<SkippedFile> SkippedFiles { get; set; } = new List<SkippedFile>();

        /// <summary>
        /// Gets or sets the distribution of parts in a multi-part archive.
        /// </summary>
        /// <remarks>
        /// When using <see cref="ProcessingMethod.MultiPart"/>, this provides
        /// information about which original files were included in each archive part.
        /// <para>
        /// This information can be useful for generating email content that explains
        /// the distribution of files across multiple parts.
        /// </para>
        /// </remarks>
        public List<PartInfo> PartDistribution { get; set; } = new List<PartInfo>();

        /// <summary>
        /// Gets or sets the error message if processing failed.
        /// </summary>
        /// <remarks>
        /// If an error occurred during processing, this property will contain a descriptive
        /// message. If processing was successful, this will be null or empty.
        /// <para>
        /// A non-empty error message implies that <see cref="Success"/> will be false.
        /// </para>
        /// </remarks>
        public string? Error { get; set; }

        /// <summary>
        /// Gets a value indicating whether the processing was successful.
        /// </summary>
        /// <remarks>
        /// A simple way to check if processing completed without errors.
        /// <para>
        /// Even if <see cref="Success"/> is true, there may still be files in the
        /// <see cref="SkippedFiles"/> list if some files were skipped but overall
        /// processing was still considered successful.
        /// </para>
        /// </remarks>
        public bool Success => string.IsNullOrEmpty(Error);
    }
}