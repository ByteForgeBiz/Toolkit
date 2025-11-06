using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

namespace ByteForge.Toolkit;
/// <summary>
/// Represents an email attachment backed by a temporary file.
/// Automatically deletes the temporary file when disposed.
/// </summary>
internal class TempFileAttachment : Attachment
{
    /// <summary>
    /// The path to the temporary file associated with this attachment.
    /// </summary>
    private readonly string? tempFilePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="TempFileAttachment"/> class using the specified file name.
    /// </summary>
    /// <param name="fileName">The path to the file to attach.</param>
    public TempFileAttachment(string fileName)
        : base(fileName) => tempFilePath = fileName;

    /// <summary>
    /// Initializes a new instance of the <see cref="TempFileAttachment"/> class using the specified file name and media type.
    /// </summary>
    /// <param name="fileName">The path to the file to attach.</param>
    /// <param name="mediaType">The MIME media type of the attachment.</param>
    public TempFileAttachment(string fileName, string mediaType)
        : base(fileName, mediaType) => tempFilePath = fileName;

    /// <summary>
    /// Initializes a new instance of the <see cref="TempFileAttachment"/> class using the specified file name and content type.
    /// </summary>
    /// <param name="fileName">The path to the file to attach.</param>
    /// <param name="contentType">The content type of the attachment.</param>
    public TempFileAttachment(string fileName, ContentType contentType)
        : base(fileName, contentType) => tempFilePath = fileName;

    /// <summary>
    /// Initializes a new instance of the <see cref="TempFileAttachment"/> class using the specified content stream and name.
    /// </summary>
    /// <param name="contentStream">The stream containing the attachment data.</param>
    /// <param name="name">The name of the attachment.</param>
    public TempFileAttachment(Stream contentStream, string name)
        : base(contentStream, name) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="TempFileAttachment"/> class using the specified content stream and content type.
    /// </summary>
    /// <param name="contentStream">The stream containing the attachment data.</param>
    /// <param name="contentType">The content type of the attachment.</param>
    public TempFileAttachment(Stream contentStream, ContentType contentType)
        : base(contentStream, contentType) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="TempFileAttachment"/> class using the specified content stream, name, and media type.
    /// </summary>
    /// <param name="contentStream">The stream containing the attachment data.</param>
    /// <param name="name">The name of the attachment.</param>
    /// <param name="mediaType">The MIME media type of the attachment.</param>
    public TempFileAttachment(Stream contentStream, string name, string mediaType)
        : base(contentStream, name, mediaType) { }

    /// <summary>
    /// Releases the resources used by the <see cref="TempFileAttachment"/> and deletes the associated temporary file if it exists.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    override protected void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try
        {
            if (File.Exists(tempFilePath))
                File.Delete(tempFilePath);
        }
        catch
        {
            // Ignore any exceptions during file deletion
        }
    }
}
