# Audio Tests

This directory contains unit tests for the ByteForge.Toolkit Data Audio module, which provides functionality for detecting and processing audio file formats.

## Overview

The Audio module includes components for identifying audio file formats, extracting metadata, and determining appropriate file extensions and MIME types. These tests ensure that audio format detection works correctly for various audio formats.

## Test Classes

### AudioFormatDetectorTests

Tests for the AudioFormatDetector class, which analyzes audio data to determine its format:

- Detection of common audio formats (MP3, WAV, FLAC, AAC, etc.)
- Extraction of file extensions based on detected formats
- Determination of MIME types for audio formats
- Handling of incomplete or corrupted audio data
- Performance with large audio files
- Edge cases such as null inputs, empty arrays
- Format mapping consistency across all supported formats

## Testing Strategy

The Audio tests follow a comprehensive approach that covers:

1. **Format detection**: Accurate identification of various audio formats
2. **Metadata extraction**: Proper reading of format-specific metadata
3. **Error handling**: Appropriate response to invalid or corrupted audio data
4. **Performance**: Acceptable processing speed for audio format detection
5. **Completeness**: Testing all supported audio formats and conversions

## Test Data

The tests use various audio file samples:

- Valid audio files in different formats
- Corrupted or incomplete audio files
- Files with unusual headers or metadata
- Edge cases with minimal valid data

## Notes

The AudioFormatDetector implements format detection based on file headers and signatures rather than relying on file extensions. This allows for accurate format identification even when files have incorrect or missing extensions.
