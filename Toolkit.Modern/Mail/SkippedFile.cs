namespace ByteForge.Toolkit.Mail;
/*
 *  ___ _   _                   _ ___ _ _     
 * / __| |_(_)_ __ _ __  ___ __| | __(_) |___ 
 * \__ \ / / | '_ \ '_ \/ -_) _` | _|| | / -_)
 * |___/_\_\_| .__/ .__/\___\__,_|_| |_|_\___|
 *           |_|  |_|                         
 */
/// <summary>
/// Information about a file that was skipped during processing.
/// </summary>
public class SkippedFile
{
    /// <summary>
    /// Gets or sets the file path of the skipped file.
    /// </summary>
    public string FilePath { get; set; } = "";

    /// <summary>
    /// Gets or sets the reason why the file was skipped.
    /// </summary>
    public string Reason { get; set; } = "";
}
