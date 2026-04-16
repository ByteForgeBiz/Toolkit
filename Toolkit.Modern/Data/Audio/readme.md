# Audio Module

## Overview

The **Audio** module provides lightweight, dependency-free detection of common audio file formats from raw byte data. It inspects binary magic bytes (file signatures) to identify the format without requiring file system access or any audio decoding library.

---

## Key Types

### `AudioFormatDetector`

**Namespace:** `ByteForge.Toolkit.Data`

A static utility class that identifies audio formats by examining the leading bytes of a byte array, then maps formats to their conventional file extensions and MIME types.

#### `AudioFormat` enum

Represents the set of supported audio formats.

| Value | Description |
|-------|-------------|
| `Unknown` | Format not recognised |
| `MP3` | MPEG Layer III (ID3v2 tag or MPEG frame sync) |
| `WAV` | Waveform Audio File Format (RIFF/WAVE header) |
| `FLAC` | Free Lossless Audio Codec (`fLaC` marker) |
| `OGG` | Ogg Vorbis (`OggS` marker) |
| `M4A` | MPEG-4 Audio (`ftyp` box) |
| `WMA` | Windows Media Audio (ASF GUID header) |
| `AIFF` | Audio Interchange File Format (FORM/AIFF header) |

#### Methods

| Method | Signature | Description |
|--------|-----------|-------------|
| `DetectFormat` | `static AudioFormat DetectFormat(byte[] audioData)` | Inspects up to the first 16 bytes and returns the matching `AudioFormat`, or `Unknown` if unrecognised. Returns `Unknown` if the array is null or shorter than 12 bytes. |
| `GetFileExtension` | `static string GetFileExtension(AudioFormat format)` | Returns the conventional file extension (including the leading dot), e.g. `".mp3"`. Returns `".bin"` for `Unknown`. |
| `GetMimeType` | `static string GetMimeType(AudioFormat format)` | Returns the IANA MIME type string, e.g. `"audio/mpeg"`. Returns `"application/octet-stream"` for `Unknown`. |

---

## Usage

```csharp
// Detect format from raw bytes (e.g. read from a stream or database BLOB)
byte[] audioData = File.ReadAllBytes("track.mp3");

AudioFormatDetector.AudioFormat format = AudioFormatDetector.DetectFormat(audioData);

if (format != AudioFormatDetector.AudioFormat.Unknown)
{
    string ext  = AudioFormatDetector.GetFileExtension(format); // ".mp3"
    string mime = AudioFormatDetector.GetMimeType(format);      // "audio/mpeg"

    Console.WriteLine($"Detected: {format} | Extension: {ext} | MIME: {mime}");
}
else
{
    Console.WriteLine("Unknown or unsupported audio format.");
}
```

### Serving audio from a web endpoint

```csharp
public FileContentResult ServeAudio(byte[] audioBlob)
{
    var format = AudioFormatDetector.DetectFormat(audioBlob);
    var mime   = AudioFormatDetector.GetMimeType(format);
    var name   = $"audio{AudioFormatDetector.GetFileExtension(format)}";

    return File(audioBlob, mime, name);
}
```

### Saving a BLOB with the correct extension

```csharp
public string SaveAudioBlob(byte[] blob, string directory)
{
    var format = AudioFormatDetector.DetectFormat(blob);
    var ext    = AudioFormatDetector.GetFileExtension(format);

    var fileName = $"{Guid.NewGuid()}{ext}";
    var path     = Path.Combine(directory, fileName);

    File.WriteAllBytes(path, blob);
    return path;
}
```

---

## Detection Logic

Detection is purely signature-based — no audio decoding takes place.

| Format | Signature bytes inspected |
|--------|--------------------------|
| MP3 | Bytes 0-2 = `49 44 33` ("ID3") **or** byte 0 = `FF`, byte 1 high 3 bits = `111` (MPEG sync) |
| WAV | Bytes 0-3 = `52 49 46 46` ("RIFF"), bytes 8-11 = `57 41 56 45` ("WAVE") |
| FLAC | Bytes 0-3 = `66 4C 61 43` ("fLaC") |
| OGG | Bytes 0-3 = `4F 67 67 53` ("OggS") |
| M4A | Bytes 4-7 = `66 74 79 70` ("ftyp") |
| WMA | Bytes 0-15 = ASF GUID `30 26 B2 75 8E 66 CF 11 A6 D9 00 AA 00 62 CE 6C` |
| AIFF | Bytes 0-3 = `46 4F 52 4D` ("FORM"), bytes 8-11 = `41 49 46 46` ("AIFF") |

---

## Limitations

- Does not validate the full file structure — only inspects leading bytes.
- M4A detection matches any MPEG-4 container (`ftyp` box); MP4 video files may produce a false positive.
- Does not extract metadata (title, bitrate, sample rate, duration, etc.).
- Detection requires at least 12 bytes; shorter arrays always return `Unknown`.

---

## 📖 Documentation Links

| Module | Description |
|--------|-------------|
| **[Data](../readme.md)** | Data module overview |
| **[CSV](../CSV/readme.md)** | CSV file processing |
| **[Database](../Database/readme.md)** | Database operations |
| **[Attributes](../Attributes/readme.md)** | Data mapping attributes |
| **[Exceptions](../Exceptions/readme.md)** | Custom exceptions |
