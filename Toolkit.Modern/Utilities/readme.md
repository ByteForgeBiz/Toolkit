# Utils

This folder contains a collection of utility classes for various common tasks such as parsing, string manipulation, I/O operations, and more.

## Classes

- **Utils**: General utility methods including US phone number formatting (with letter-to-number conversion), null-if-empty checks, and synchronous execution of async functions.

- **StringUtil**: String manipulation utilities like escaping for JavaScript, wrapping comma-separated lists to fit line lengths, and splitting PascalCase strings into words.

- **IParser**: Interface defining methods for parsing strings into various data types, with support for custom type registration.

- **Parser**: Implementation of IParser, providing parsing for common types like bool, int, DateTime, etc., with extensible custom parsers.

- **BooleanParser**: Specialized parser for boolean values, handling various string representations.

- **DateTimeParser**: Parser for DateTime objects from strings.

- **DateTimeUtil**: Utilities for DateTime operations, such as comparisons and formatting.

- **EnumExtensions**: Extension methods for enums, like parsing from strings and getting descriptions.

- **HtmlUtil**: Utilities for HTML manipulation, such as encoding/decoding.

- **IOUtils**: Input/Output utilities for file and directory operations.

- **NameUtil**: Utilities for name processing, like capitalizing or formatting names.

- **TemplateProcessor**: Class for processing templates with placeholders.

- **TimingUtil**: Utilities for timing operations, like measuring execution time.

- **TypeHelper**: Helpers for type reflection and manipulation.

- **ValueConverterRegistry**: Registry for value converters between types.

See also: Individual class files for detailed documentation and the Toolkit.Modern.Tests project for usage examples.

---

## 📖 Documentation Links

### 🏗️ Related Modules
| Module                                            | Description                |
|---------------------------------------------------|----------------------------|
| **[CLI](../CommandLine/readme.md)**               | Command-line parsing       |
| **[Configuration](../Configuration/readme.md)**   | INI-based configuration    |
| **[Core](../Core/readme.md)**                     | Core utilities             |
| **[Data](../Data/readme.md)**                     | Database & file processing |
| **[DataStructures](../DataStructures/readme.md)** | Collections & utilities    |
| **[JSON](../Json/readme.md)**                     | Delta serialization        |
| **[Logging](../Logging/readme.md)**               | Structured logging         |
| **[Mail](../Mail/readme.md)**                     | Email processing           |
| **[Net](../Net/readme.md)**                       | Network file transfers     |
| **[Security](../Security/readme.md)**             | Encryption & security      |
