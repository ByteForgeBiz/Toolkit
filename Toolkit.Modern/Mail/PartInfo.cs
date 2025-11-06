namespace ByteForge.Toolkit;
/*
 *  ___          _   ___       __     
 * | _ \__ _ _ _| |_|_ _|_ _  / _|___ 
 * |  _/ _` | '_|  _|| || ' \|  _/ _ \
 * |_| \__,_|_|  \__|___|_||_|_| \___/
 *                                    
 */
/// <summary>
/// Information about a part in a multi-part archive created during email attachment processing.
/// </summary>
/// <remarks>
/// When files are too large to be sent as a single attachment, they may be split into multiple
/// archive parts. This class stores metadata about each part, including which files were
/// included and the sequential part number.
/// <para>
/// PartInfo objects are typically created by the <see cref="EmailAttachmentHandler"/> when
/// using the <see cref="ProcessingMethod.MultiPart"/> method and are returned
/// in the <see cref="AttachmentProcessResult.PartDistribution"/> property.
/// </para>
/// <para>
/// This information can be useful for generating emails that describe which files are 
/// included in each archive part or for tracking the distribution of files across
/// multiple message attachments.
/// </para>
/// </remarks>
public class PartInfo
{
    /// <summary>
    /// Gets or sets the part number.
    /// </summary>
    /// <remarks>
    /// The part number is a 1-based index that represents the sequence
    /// of this part in the multi-part archive. For example, part 1 of 3.
    /// </remarks>
    public int PartNumber { get; set; }

    /// <summary>
    /// Gets or sets the number of files in the part.
    /// </summary>
    /// <remarks>
    /// This is a convenience property that returns the number of files
    /// listed in the <see cref="Files"/> collection. It represents the 
    /// number of original files that were added to this part of the archive.
    /// </remarks>
    public int FileCount { get; set; }

    /// <summary>
    /// Gets or sets the list of file names in the part.
    /// </summary>
    /// <remarks>
    /// Contains the names of the original files that were included in this part
    /// of the archive. These are not the archive files themselves, but rather the
    /// source files that were compressed into this archive part.
    /// </remarks>
    public List<string> Files { get; set; } = new List<string>();
}
