using System.Collections.Generic;
using System.IO;

namespace ByteForge.Toolkit
{
    public partial class EmailAttachmentHandler
    {
        /*
         *  ___ _ _     ___         _       _   
         * | __(_) |___| _ )_  _ __| |_____| |_ 
         * | _|| | / -_) _ \ || / _| / / -_)  _|
         * |_| |_|_\___|___/\_,_\__|_\_\___|\__|
         *                                      
         */
        /// <summary>
        /// Helper class for file distribution into buckets when creating multi-part archives.
        /// </summary>
        /// <remarks>
        /// FileBucket facilitates the intelligent distribution of files into separate buckets,
        /// optimizing the size and number of parts in multi-part archive creation.
        /// This class helps balance file sizes across multiple archives while maintaining
        /// logical grouping of related files when possible.
        /// <para>
        /// Each bucket represents a potential archive part and tracks its total size
        /// to ensure efficient use of storage and transmission capacity.
        /// </para>
        /// </remarks>
        private class FileBucket
        {
            /// <summary>
            /// Gets or sets the index of the bucket.
            /// </summary>
            /// <remarks>
            /// The bucket index corresponds to the part number in multi-part archives.
            /// </remarks>
            public int Index { get; set; }

            /// <summary>
            /// Gets or sets the total size of the files in the bucket.
            /// </summary>
            /// <remarks>
            /// Measured in bytes. This is used to balance files across buckets
            /// to create archives of similar sizes.
            /// </remarks>
            public long TotalSize { get; set; } = 0;

            /// <summary>
            /// Gets or sets the list of files in the bucket.
            /// </summary>
            /// <remarks>
            /// Contains the file information for all files assigned to this bucket.
            /// These files will typically be archived together in a single part.
            /// </remarks>
            public List<FileInfo> Files { get; set; } = new List<FileInfo>();
        }
    }
}