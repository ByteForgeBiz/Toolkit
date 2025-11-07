using AwesomeAssertions;
using ByteForge.Toolkit.Data;

namespace ByteForge.Toolkit.Tests.Unit.Data.Audio
{
    [TestClass]
    [TestCategory("Unit")]
    [TestCategory("Data")]
    [TestCategory("Audio")]
    public class AudioFormatDetectorTests
    {
        #region Test Audio File Headers

        /// <summary>
        /// MP3 file headers for testing.
        /// </summary>
        private static readonly byte[] Mp3HeaderId3 = [0x49, 0x44, 0x33, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]; // "ID3"
        private static readonly byte[] Mp3HeaderMpeg = [0xFF, 0xFB, 0x90, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]; // MPEG frame sync

        /// <summary>
        /// WAV file header for testing.
        /// </summary>
        private static readonly byte[] WavHeader = [0x52, 0x49, 0x46, 0x46, 0x24, 0x08, 0x00, 0x00, 0x57, 0x41, 0x56, 0x45]; // "RIFF...WAVE"

        /// <summary>
        /// FLAC file header for testing.
        /// </summary>
        private static readonly byte[] FlacHeader = [0x66, 0x4C, 0x61, 0x43, 0x00, 0x00, 0x00, 0x22, 0x10, 0x00, 0x10, 0x00]; // "fLaC"

        /// <summary>
        /// OGG file header for testing.
        /// </summary>
        private static readonly byte[] OggHeader = [0x4F, 0x67, 0x67, 0x53, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]; // "OggS"

        /// <summary>
        /// M4A file header for testing.
        /// </summary>
        private static readonly byte[] M4aHeader = [0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70, 0x4D, 0x34, 0x41, 0x20]; // "ftyp"

        /// <summary>
        /// WMA file header for testing (ASF GUID).
        /// </summary>
        private static readonly byte[] WmaHeader = [0x30, 0x26, 0xB2, 0x75, 0x8E, 0x66, 0xCF, 0x11, 0xA6, 0xD9, 0x00, 0xAA, 0x00, 0x62, 0xCE, 0x6C];

        /// <summary>
        /// AIFF file header for testing.
        /// </summary>
        private static readonly byte[] AiffHeader = [0x46, 0x4F, 0x52, 0x4D, 0x00, 0x00, 0x0F, 0x34, 0x41, 0x49, 0x46, 0x46]; // "FORM...AIFF"

        #endregion

        #region DetectFormat Method Tests

        /// <summary>
        /// Verifies that DetectFormat correctly identifies MP3 format with ID3 header.
        /// </summary>
        /// <remarks>
        /// Tests MP3 detection using ID3v2 tag header.
        /// </remarks>
        [TestMethod]
        public void DetectFormat_Mp3WithId3Header_ShouldReturnMp3()
        {
            // Act
            var result = AudioFormatDetector.DetectFormat(Mp3HeaderId3);

            // Assert
            result.Should().Be(AudioFormatDetector.AudioFormat.MP3, "should detect MP3 format from ID3 header");
        }

        /// <summary>
        /// Verifies that DetectFormat correctly identifies MP3 format with MPEG frame sync.
        /// </summary>
        /// <remarks>
        /// Tests MP3 detection using MPEG frame synchronization pattern.
        /// </remarks>
        [TestMethod]
        public void DetectFormat_Mp3WithMpegSync_ShouldReturnMp3()
        {
            // Act
            var result = AudioFormatDetector.DetectFormat(Mp3HeaderMpeg);

            // Assert
            result.Should().Be(AudioFormatDetector.AudioFormat.MP3, "should detect MP3 format from MPEG frame sync");
        }

        /// <summary>
        /// Verifies that DetectFormat correctly identifies WAV format.
        /// </summary>
        /// <remarks>
        /// Tests WAV detection using RIFF/WAVE header.
        /// </remarks>
        [TestMethod]
        public void DetectFormat_WavHeader_ShouldReturnWav()
        {
            // Act
            var result = AudioFormatDetector.DetectFormat(WavHeader);

            // Assert
            result.Should().Be(AudioFormatDetector.AudioFormat.WAV, "should detect WAV format from RIFF/WAVE header");
        }

        /// <summary>
        /// Verifies that DetectFormat correctly identifies FLAC format.
        /// </summary>
        /// <remarks>
        /// Tests FLAC detection using fLaC signature.
        /// </remarks>
        [TestMethod]
        public void DetectFormat_FlacHeader_ShouldReturnFlac()
        {
            // Act
            var result = AudioFormatDetector.DetectFormat(FlacHeader);

            // Assert
            result.Should().Be(AudioFormatDetector.AudioFormat.FLAC, "should detect FLAC format from fLaC header");
        }

        /// <summary>
        /// Verifies that DetectFormat correctly identifies OGG format.
        /// </summary>
        /// <remarks>
        /// Tests OGG detection using OggS signature.
        /// </remarks>
        [TestMethod]
        public void DetectFormat_OggHeader_ShouldReturnOgg()
        {
            // Act
            var result = AudioFormatDetector.DetectFormat(OggHeader);

            // Assert
            result.Should().Be(AudioFormatDetector.AudioFormat.OGG, "should detect OGG format from OggS header");
        }

        /// <summary>
        /// Verifies that DetectFormat correctly identifies M4A format.
        /// </summary>
        /// <remarks>
        /// Tests M4A detection using ftyp box signature.
        /// </remarks>
        [TestMethod]
        public void DetectFormat_M4aHeader_ShouldReturnM4a()
        {
            // Act
            var result = AudioFormatDetector.DetectFormat(M4aHeader);

            // Assert
            result.Should().Be(AudioFormatDetector.AudioFormat.M4A, "should detect M4A format from ftyp header");
        }

        /// <summary>
        /// Verifies that DetectFormat correctly identifies WMA format.
        /// </summary>
        /// <remarks>
        /// Tests WMA detection using ASF GUID header.
        /// </remarks>
        [TestMethod]
        public void DetectFormat_WmaHeader_ShouldReturnWma()
        {
            // Act
            var result = AudioFormatDetector.DetectFormat(WmaHeader);

            // Assert
            result.Should().Be(AudioFormatDetector.AudioFormat.WMA, "should detect WMA format from ASF GUID header");
        }

        /// <summary>
        /// Verifies that DetectFormat correctly identifies AIFF format.
        /// </summary>
        /// <remarks>
        /// Tests AIFF detection using FORM/AIFF header.
        /// </remarks>
        [TestMethod]
        public void DetectFormat_AiffHeader_ShouldReturnAiff()
        {
            // Act
            var result = AudioFormatDetector.DetectFormat(AiffHeader);

            // Assert
            result.Should().Be(AudioFormatDetector.AudioFormat.AIFF, "should detect AIFF format from FORM/AIFF header");
        }

        /// <summary>
        /// Verifies that DetectFormat returns Unknown for null input.
        /// </summary>
        /// <remarks>
        /// Tests null input validation.
        /// </remarks>
        [TestMethod]
        public void DetectFormat_NullInput_ShouldReturnUnknown()
        {
            // Act
            var result = AudioFormatDetector.DetectFormat(null);

            // Assert
            result.Should().Be(AudioFormatDetector.AudioFormat.Unknown, "should return Unknown for null input");
        }

        /// <summary>
        /// Verifies that DetectFormat returns Unknown for empty array.
        /// </summary>
        /// <remarks>
        /// Tests empty array input validation.
        /// </remarks>
        [TestMethod]
        public void DetectFormat_EmptyArray_ShouldReturnUnknown()
        {
            // Act
            var result = AudioFormatDetector.DetectFormat([]);

            // Assert
            result.Should().Be(AudioFormatDetector.AudioFormat.Unknown, "should return Unknown for empty array");
        }

        /// <summary>
        /// Verifies that DetectFormat returns Unknown for insufficient data.
        /// </summary>
        /// <remarks>
        /// Tests minimum data length requirement (12 bytes).
        /// </remarks>
        [TestMethod]
        public void DetectFormat_InsufficientData_ShouldReturnUnknown()
        {
            // Arrange
            var shortData = "ID3"u8.ToArray(); // Only 3 bytes

            // Act
            var result = AudioFormatDetector.DetectFormat(shortData);

            // Assert
            result.Should().Be(AudioFormatDetector.AudioFormat.Unknown, "should return Unknown for insufficient data");
        }

        /// <summary>
        /// Verifies that DetectFormat returns Unknown for unrecognized format.
        /// </summary>
        /// <remarks>
        /// Tests behavior with random or unknown audio data.
        /// </remarks>
        [TestMethod]
        public void DetectFormat_UnrecognizedFormat_ShouldReturnUnknown()
        {
            // Arrange
            var randomData = new byte[] { 0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0, 0x11, 0x22, 0x33, 0x44 };

            // Act
            var result = AudioFormatDetector.DetectFormat(randomData);

            // Assert
            result.Should().Be(AudioFormatDetector.AudioFormat.Unknown, "should return Unknown for unrecognized format");
        }

        /// <summary>
        /// Verifies that DetectFormat handles partial headers correctly.
        /// </summary>
        /// <remarks>
        /// Tests behavior when header is partially present but incomplete.
        /// </remarks>
        [TestMethod]
        public void DetectFormat_PartialHeaders_ShouldReturnUnknown()
        {
            // Arrange
            var partialWav = new byte[] { 0x52, 0x49, 0x46, 0x46, 0x24, 0x08, 0x00, 0x00, 0x57, 0x41, 0x56 }; // Missing last byte
            var partialFlac = "fLa"u8.ToArray(); // Only "fLa"

            // Act & Assert
            AudioFormatDetector.DetectFormat(partialWav).Should().Be(AudioFormatDetector.AudioFormat.Unknown, "should return Unknown for partial WAV header");
            AudioFormatDetector.DetectFormat(partialFlac).Should().Be(AudioFormatDetector.AudioFormat.Unknown, "should return Unknown for partial FLAC header");
        }

        /// <summary>
        /// Verifies that DetectFormat prioritizes format detection correctly.
        /// </summary>
        /// <remarks>
        /// Tests that the first matching format is returned when multiple patterns could match.
        /// </remarks>
        [TestMethod]
        public void DetectFormat_FormatPriority_ShouldReturnFirstMatch()
        {
            // Arrange - Create data that could potentially match MP3 (checked first)
            var ambiguousData = new byte[20];
            ambiguousData[0] = 0x49; // 'I'
            ambiguousData[1] = 0x44; // 'D'
            ambiguousData[2] = 0x33; // '3' - This makes it MP3 (ID3)

            // Act
            var result = AudioFormatDetector.DetectFormat(ambiguousData);

            // Assert
            result.Should().Be(AudioFormatDetector.AudioFormat.MP3, "should return first matching format (MP3 in this case)");
        }

        #endregion

        #region GetFileExtension Method Tests

        /// <summary>
        /// Verifies that GetFileExtension returns correct extensions for all audio formats.
        /// </summary>
        /// <remarks>
        /// Tests file extension mapping for each supported format.
        /// </remarks>
        [TestMethod]
        public void GetFileExtension_AllFormats_ShouldReturnCorrectExtensions()
        {
            // Arrange & Act & Assert
            AudioFormatDetector.GetFileExtension(AudioFormatDetector.AudioFormat.MP3)
                .Should().Be(".mp3", "should return correct extension for MP3");

            AudioFormatDetector.GetFileExtension(AudioFormatDetector.AudioFormat.WAV)
                .Should().Be(".wav", "should return correct extension for WAV");

            AudioFormatDetector.GetFileExtension(AudioFormatDetector.AudioFormat.FLAC)
                .Should().Be(".flac", "should return correct extension for FLAC");

            AudioFormatDetector.GetFileExtension(AudioFormatDetector.AudioFormat.OGG)
                .Should().Be(".ogg", "should return correct extension for OGG");

            AudioFormatDetector.GetFileExtension(AudioFormatDetector.AudioFormat.M4A)
                .Should().Be(".m4a", "should return correct extension for M4A");

            AudioFormatDetector.GetFileExtension(AudioFormatDetector.AudioFormat.WMA)
                .Should().Be(".wma", "should return correct extension for WMA");

            AudioFormatDetector.GetFileExtension(AudioFormatDetector.AudioFormat.AIFF)
                .Should().Be(".aiff", "should return correct extension for AIFF");
        }

        /// <summary>
        /// Verifies that GetFileExtension returns ".bin" for Unknown format.
        /// </summary>
        /// <remarks>
        /// Tests default extension for unknown format.
        /// </remarks>
        [TestMethod]
        public void GetFileExtension_UnknownFormat_ShouldReturnBin()
        {
            // Act
            var result = AudioFormatDetector.GetFileExtension(AudioFormatDetector.AudioFormat.Unknown);

            // Assert
            result.Should().Be(".bin", "should return .bin for unknown format");
        }

        /// <summary>
        /// Verifies that GetFileExtension handles invalid enum values gracefully.
        /// </summary>
        /// <remarks>
        /// Tests behavior with cast invalid enum values.
        /// </remarks>
        [TestMethod]
        public void GetFileExtension_InvalidEnumValue_ShouldReturnBin()
        {
            // Arrange
            var invalidFormat = (AudioFormatDetector.AudioFormat)999;

            // Act
            var result = AudioFormatDetector.GetFileExtension(invalidFormat);

            // Assert
            result.Should().Be(".bin", "should return .bin for invalid enum values");
        }

        #endregion

        #region GetMimeType Method Tests

        /// <summary>
        /// Verifies that GetMimeType returns correct MIME types for all audio formats.
        /// </summary>
        /// <remarks>
        /// Tests MIME type mapping for each supported format.
        /// </remarks>
        [TestMethod]
        public void GetMimeType_AllFormats_ShouldReturnCorrectMimeTypes()
        {
            // Arrange & Act & Assert
            AudioFormatDetector.GetMimeType(AudioFormatDetector.AudioFormat.MP3)
                .Should().Be("audio/mpeg", "should return correct MIME type for MP3");

            AudioFormatDetector.GetMimeType(AudioFormatDetector.AudioFormat.WAV)
                .Should().Be("audio/wav", "should return correct MIME type for WAV");

            AudioFormatDetector.GetMimeType(AudioFormatDetector.AudioFormat.FLAC)
                .Should().Be("audio/flac", "should return correct MIME type for FLAC");

            AudioFormatDetector.GetMimeType(AudioFormatDetector.AudioFormat.OGG)
                .Should().Be("audio/ogg", "should return correct MIME type for OGG");

            AudioFormatDetector.GetMimeType(AudioFormatDetector.AudioFormat.M4A)
                .Should().Be("audio/mp4", "should return correct MIME type for M4A");

            AudioFormatDetector.GetMimeType(AudioFormatDetector.AudioFormat.WMA)
                .Should().Be("audio/x-ms-wma", "should return correct MIME type for WMA");

            AudioFormatDetector.GetMimeType(AudioFormatDetector.AudioFormat.AIFF)
                .Should().Be("audio/aiff", "should return correct MIME type for AIFF");
        }

        /// <summary>
        /// Verifies that GetMimeType returns "application/octet-stream" for Unknown format.
        /// </summary>
        /// <remarks>
        /// Tests default MIME type for unknown format.
        /// </remarks>
        [TestMethod]
        public void GetMimeType_UnknownFormat_ShouldReturnOctetStream()
        {
            // Act
            var result = AudioFormatDetector.GetMimeType(AudioFormatDetector.AudioFormat.Unknown);

            // Assert
            result.Should().Be("application/octet-stream", "should return octet-stream for unknown format");
        }

        /// <summary>
        /// Verifies that GetMimeType handles invalid enum values gracefully.
        /// </summary>
        /// <remarks>
        /// Tests behavior with cast invalid enum values.
        /// </remarks>
        [TestMethod]
        public void GetMimeType_InvalidEnumValue_ShouldReturnOctetStream()
        {
            // Arrange
            var invalidFormat = (AudioFormatDetector.AudioFormat)999;

            // Act
            var result = AudioFormatDetector.GetMimeType(invalidFormat);

            // Assert
            result.Should().Be("application/octet-stream", "should return octet-stream for invalid enum values");
        }

        #endregion

        #region Integration and Edge Case Tests

        /// <summary>
        /// Verifies end-to-end workflow: detection, extension, and MIME type.
        /// </summary>
        /// <remarks>
        /// Tests complete workflow with real audio file headers.
        /// </remarks>
        [TestMethod]
        public void EndToEndWorkflow_RealAudioHeaders_ShouldWorkCorrectly()
        {
            // Arrange
            var testCases = new[]
            {
                (Mp3HeaderId3, AudioFormatDetector.AudioFormat.MP3, ".mp3", "audio/mpeg"),
                (WavHeader, AudioFormatDetector.AudioFormat.WAV, ".wav", "audio/wav"),
                (FlacHeader, AudioFormatDetector.AudioFormat.FLAC, ".flac", "audio/flac"),
                (OggHeader, AudioFormatDetector.AudioFormat.OGG, ".ogg", "audio/ogg"),
                (M4aHeader, AudioFormatDetector.AudioFormat.M4A, ".m4a", "audio/mp4")
            };

            foreach (var (header, expectedFormat, expectedExt, expectedMime) in testCases)
            {
                // Act
                var detectedFormat = AudioFormatDetector.DetectFormat(header);
                var extension = AudioFormatDetector.GetFileExtension(detectedFormat);
                var mimeType = AudioFormatDetector.GetMimeType(detectedFormat);

                // Assert
                detectedFormat.Should().Be(expectedFormat, $"should detect {expectedFormat} format");
                extension.Should().Be(expectedExt, $"should return {expectedExt} extension");
                mimeType.Should().Be(expectedMime, $"should return {expectedMime} MIME type");
            }
        }

        /// <summary>
        /// Verifies that large audio data is handled efficiently.
        /// </summary>
        /// <remarks>
        /// Performance test with large byte arrays.
        /// </remarks>
        [TestMethod]
        public void DetectFormat_LargeAudioData_ShouldHandleEfficiently()
        {
            // Arrange - Create large array with MP3 header at the beginning
            var largeAudioData = new byte[1000000]; // 1MB
            Array.Copy(Mp3HeaderId3, largeAudioData, Mp3HeaderId3.Length);
            var startTime = DateTime.UtcNow;

            // Act
            var result = AudioFormatDetector.DetectFormat(largeAudioData);

            // Assert
            var duration = DateTime.UtcNow - startTime;
            result.Should().Be(AudioFormatDetector.AudioFormat.MP3, "should detect MP3 format in large data");
            duration.Should().BeLessThan(TimeSpan.FromMilliseconds(100), "should handle large data efficiently");
        }

        /// <summary>
        /// Verifies that DetectFormat handles corrupted headers gracefully.
        /// </summary>
        /// <remarks>
        /// Tests behavior with slightly corrupted audio headers.
        /// </remarks>
        [TestMethod]
        public void DetectFormat_CorruptedHeaders_ShouldHandleGracefully()
        {
            // Arrange - Create corrupted versions of known headers
            var corruptedMp3 = new byte[Mp3HeaderId3.Length];
            Array.Copy(Mp3HeaderId3, corruptedMp3, Mp3HeaderId3.Length);
            corruptedMp3[1] = 0x00; // Corrupt the 'D' in "ID3"

            var corruptedWav = new byte[WavHeader.Length];
            Array.Copy(WavHeader, corruptedWav, WavHeader.Length);
            corruptedWav[8] = 0x00; // Corrupt the 'W' in "WAVE"

            // Act & Assert
            AudioFormatDetector.DetectFormat(corruptedMp3).Should().Be(AudioFormatDetector.AudioFormat.Unknown, "should return Unknown for corrupted MP3 header");
            AudioFormatDetector.DetectFormat(corruptedWav).Should().Be(AudioFormatDetector.AudioFormat.Unknown, "should return Unknown for corrupted WAV header");
        }

        /// <summary>
        /// Verifies that all enum values are properly handled by utility methods.
        /// </summary>
        /// <remarks>
        /// Tests that all enum values have corresponding extension and MIME type mappings.
        /// </remarks>
        [TestMethod]
        public void AllEnumValues_ShouldHaveProperMappings()
        {
            // Arrange - Get all enum values
            var enumValues = Enum.GetValues(typeof(AudioFormatDetector.AudioFormat));

            foreach (AudioFormatDetector.AudioFormat format in enumValues)
            {
                // Act
                var extension = AudioFormatDetector.GetFileExtension(format);
                var mimeType = AudioFormatDetector.GetMimeType(format);

                // Assert
                extension.Should().NotBeNullOrEmpty($"should have extension mapping for {format}");
                extension.Should().StartWith(".", $"extension for {format} should start with dot");
                mimeType.Should().NotBeNullOrEmpty($"should have MIME type mapping for {format}");
            }
        }

        /// <summary>
        /// Performance test for repeated format detection calls.
        /// </summary>
        /// <remarks>
        /// Tests performance with multiple repeated detection operations.
        /// </remarks>
        [TestMethod]
        public void DetectFormat_RepeatedCalls_ShouldHandleEfficiently()
        {
            // Arrange
            var iterations = 10000;
            var startTime = DateTime.UtcNow;

            // Act
            for (var i = 0; i < iterations; i++)
            {
                var format1 = AudioFormatDetector.DetectFormat(Mp3HeaderId3);
                var format2 = AudioFormatDetector.DetectFormat(WavHeader);
                var format3 = AudioFormatDetector.DetectFormat(FlacHeader);
                
                // Verify results to ensure compiler doesn't optimize away
                format1.Should().Be(AudioFormatDetector.AudioFormat.MP3);
                format2.Should().Be(AudioFormatDetector.AudioFormat.WAV);
                format3.Should().Be(AudioFormatDetector.AudioFormat.FLAC);
            }

            // Assert
            var duration = DateTime.UtcNow - startTime;
            duration.Should().BeLessThan(TimeSpan.FromSeconds(3), 
                $"should handle {iterations * 3} detection operations efficiently");
        }

        #endregion
    }
}