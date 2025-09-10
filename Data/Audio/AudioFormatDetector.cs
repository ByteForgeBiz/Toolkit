namespace ByteForge.Toolkit
{
    /*
     *    _          _ _     ___                   _   ___      _          _           
     *   /_\ _  _ __| (_)___| __|__ _ _ _ __  __ _| |_|   \ ___| |_ ___ __| |_ ___ _ _ 
     *  / _ \ || / _` | / _ \ _/ _ \ '_| '  \/ _` |  _| |) / -_)  _/ -_) _|  _/ _ \ '_|
     * /_/ \_\_,_\__,_|_\___/_|\___/_| |_|_|_\__,_|\__|___/\___|\__\___\__|\__\___/_|  
     *                                                                                 
     */
    /// <summary>
    /// Provides methods to detect audio file formats from raw data and retrieve related information.
    /// </summary>
    public class AudioFormatDetector
    {
        /// <summary>
        /// Represents supported audio formats.
        /// </summary>
        public enum AudioFormat
        {
            /// <summary>
            /// Unknown or unsupported audio format.
            /// </summary>
            Unknown,
            /// <summary>
            /// MPEG Layer III Audio (MP3).
            /// </summary>
            MP3,
            /// <summary>
            /// Waveform Audio File Format (WAV).
            /// </summary>
            WAV,
            /// <summary>
            /// Free Lossless Audio Codec (FLAC).
            /// </summary>
            FLAC,
            /// <summary>
            /// Ogg Vorbis Audio (OGG).
            /// </summary>
            OGG,
            /// <summary>
            /// MPEG-4 Audio (M4A).
            /// </summary>
            M4A,
            /// <summary>
            /// Windows Media Audio (WMA).
            /// </summary>
            WMA,
            /// <summary>
            /// Audio Interchange File Format (AIFF).
            /// </summary>
            AIFF
        }

        /// <summary>
        /// Detects the audio format of the provided raw audio data.
        /// </summary>
        /// <param name="audioData">The raw audio data as a byte array.</param>
        /// <returns>
        /// The detected <see cref="AudioFormat"/> value, or <see cref="AudioFormat.Unknown"/> if the format is not recognized.
        /// </returns>
        public static AudioFormat DetectFormat(byte[] audioData)
        {
            if (audioData == null || audioData.Length < 12)
                return AudioFormat.Unknown;

            // MP3 - ID3v2 tag or MPEG frame sync
            if (audioData.Length >= 3 &&
                audioData[0] == 0x49 && audioData[1] == 0x44 && audioData[2] == 0x33) // "ID3"
                return AudioFormat.MP3;

            if (audioData.Length >= 2 &&
                audioData[0] == 0xFF && (audioData[1] & 0xE0) == 0xE0) // MPEG frame sync
                return AudioFormat.MP3;

            // WAV - RIFF header
            if (audioData.Length >= 12 &&
                audioData[0] == 0x52 && audioData[1] == 0x49 &&
                audioData[2] == 0x46 && audioData[3] == 0x46 && // "RIFF"
                audioData[8] == 0x57 && audioData[9] == 0x41 &&
                audioData[10] == 0x56 && audioData[11] == 0x45) // "WAVE"
                return AudioFormat.WAV;

            // FLAC
            if (audioData.Length >= 4 &&
                audioData[0] == 0x66 && audioData[1] == 0x4C &&
                audioData[2] == 0x61 && audioData[3] == 0x43) // "fLaC"
                return AudioFormat.FLAC;

            // OGG
            if (audioData.Length >= 4 &&
                audioData[0] == 0x4F && audioData[1] == 0x67 &&
                audioData[2] == 0x67 && audioData[3] == 0x53) // "OggS"
                return AudioFormat.OGG;

            // M4A/MP4 - ftyp box
            if (audioData.Length >= 8 &&
                audioData[4] == 0x66 && audioData[5] == 0x74 &&
                audioData[6] == 0x79 && audioData[7] == 0x70) // "ftyp"
                return AudioFormat.M4A;

            // WMA - ASF header
            if (audioData.Length >= 16)
            {
                byte[] asfGuid = { 0x30, 0x26, 0xB2, 0x75, 0x8E, 0x66, 0xCF, 0x11,
                              0xA6, 0xD9, 0x00, 0xAA, 0x00, 0x62, 0xCE, 0x6C };
                var isAsf = true;
                for (var i = 0; i < 16; i++)
                {
                    if (audioData[i] != asfGuid[i])
                    {
                        isAsf = false;
                        break;
                    }
                }
                if (isAsf)
                    return AudioFormat.WMA;
            }

            // AIFF
            if (audioData.Length >= 12 &&
                audioData[0] == 0x46 && audioData[1] == 0x4F &&
                audioData[2] == 0x52 && audioData[3] == 0x4D && // "FORM"
                audioData[8] == 0x41 && audioData[9] == 0x49 &&
                audioData[10] == 0x46 && audioData[11] == 0x46) // "AIFF"
                return AudioFormat.AIFF;

            return AudioFormat.Unknown;
        }

        /// <summary>
        /// Gets the typical file extension for the specified audio format.
        /// </summary>
        /// <param name="format">The audio format.</param>
        /// <returns>
        /// The file extension (including the leading dot), or ".bin" if the format is unknown.
        /// </returns>
        public static string GetFileExtension(AudioFormat format)
        {
            switch (format)
            {
                case AudioFormat.MP3:
                    return ".mp3";
                case AudioFormat.WAV:
                    return ".wav";
                case AudioFormat.FLAC:
                    return ".flac";
                case AudioFormat.OGG:
                    return ".ogg";
                case AudioFormat.M4A:
                    return ".m4a";
                case AudioFormat.WMA:
                    return ".wma";
                case AudioFormat.AIFF:
                    return ".aiff";
                default:
                    return ".bin";
            }
        }

        /// <summary>
        /// Gets the MIME type for the specified audio format.
        /// </summary>
        /// <param name="format">The audio format.</param>
        /// <returns>
        /// The MIME type string, or "application/octet-stream" if the format is unknown.
        /// </returns>
        public static string GetMimeType(AudioFormat format)
        {
            switch (format)
            {
                case AudioFormat.MP3:
                    return "audio/mpeg";
                case AudioFormat.WAV:
                    return "audio/wav";
                case AudioFormat.FLAC:
                    return "audio/flac";
                case AudioFormat.OGG:
                    return "audio/ogg";
                case AudioFormat.M4A:
                    return "audio/mp4";
                case AudioFormat.WMA:
                    return "audio/x-ms-wma";
                case AudioFormat.AIFF:
                    return "audio/aiff";
                default:
                    return "application/octet-stream";
            }
        }
    }
}
