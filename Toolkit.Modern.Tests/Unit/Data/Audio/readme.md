# Audio Unit Tests

Tests for `AudioFormatDetector` in `ByteForge.Toolkit.Data`.

**Test class:** `AudioFormatDetectorTests`
**Test categories:** `Unit`, `Data`, `Audio`
**Source module:** `Toolkit.Modern/Data/`

## What Is Tested

`AudioFormatDetector` identifies audio file formats by inspecting raw byte headers rather than file extensions. It exposes a static `DetectFormat(byte[])` method returning an `AudioFormat` enum value, plus helpers for MIME type and file extension lookup.

### Format Detection (`DetectFormat`)

| Format | Header Pattern |
|--------|---------------|
| MP3 | ID3v2 tag `"ID3"` prefix |
| MP3 | MPEG frame sync `0xFF 0xFB` |
| WAV | `"RIFF"` + `"WAVE"` |
| FLAC | `"fLaC"` |
| OGG | `"OggS"` |
| M4A | `"ftyp"` box |
| WMA | ASF GUID `30 26 B2 75 ...` |
| AIFF | `"FORM"` + `"AIFF"` |

All eight format headers above are defined as inline `byte[]` constants in the test class and passed directly to `DetectFormat` — no audio files on disk are required.

### Additional Coverage

| Test area | Description |
|-----------|-------------|
| Null/empty input | `DetectFormat(null)` and `DetectFormat([])` must not throw; expected return value is `Unknown` |
| Truncated data | Arrays shorter than a complete header return `Unknown` or the correct partial-match result |
| MIME type mapping | `GetMimeType(AudioFormat)` returns the correct MIME string for every defined format |
| Extension mapping | `GetFileExtension(AudioFormat)` returns the correct extension for every defined format |
| Mapping consistency | MIME type and extension mappings cover all `AudioFormat` enum values |

## Prerequisites

No external files or databases required. All test data is constructed inline as `byte[]` constants.

## Running These Tests

```powershell
# All audio tests
dotnet test --filter "TestCategory=Audio"

# By class name
dotnet test --filter "FullyQualifiedName~AudioFormatDetectorTests"
```

---

## Documentation Links

| Location | Description | Documentation |
|----------|-------------|---------------|
| **Tests root** | Test project overview | [../../../README.md](../../../README.md) |
| **Unit overview** | Unit test organization | [../../readme.md](../../readme.md) |
| **Data overview** | Data tests overview | [../readme.md](../readme.md) |
