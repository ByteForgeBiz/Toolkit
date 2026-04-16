# Configuration / Attributes

This folder contains the attributes that control how typed section classes are mapped to INI file content.

All attributes target `Property` members of POCO classes passed to `Configuration.GetSection<T>()`.

---

## Attributes

### `ArrayAttribute`

Marks a property as a collection stored in a dedicated INI section rather than as a single key-value pair.

| Property | Type | Description |
|---|---|---|
| `SectionName` | `string` | Custom section name. If empty, defaults to `"{ParentSection}#{PropertyName}Array"`. |

Supported property types: `T[]`, `List<T>`, `IList<T>`, `ICollection<T>`, `IEnumerable<T>`.

```csharp
[Array]
public string[] AllowedHosts { get; set; }

[Array("MyCustomSection")]
public List<int> RetryDelays { get; set; }
```

---

### `DictionaryAttribute`

Marks a `Dictionary<string, string>` (or compatible) property as a collection stored in a dedicated INI section.

| Property | Type | Description |
|---|---|---|
| `SectionName` | `string` | Custom section name. If empty, defaults to `"{ParentSection}#{PropertyName}Dict"`. |

Supported property types: `Dictionary<string,string>`, `IDictionary<string,string>`, `IReadOnlyDictionary<string,string>`, `ICollection<KeyValuePair<string,string>>`, `IEnumerable<KeyValuePair<string,string>>`, `IReadOnlyCollection<KeyValuePair<string,string>>`.

```csharp
[Dictionary]
public Dictionary<string, string> HttpHeaders { get; set; }

[Dictionary("RequestHeaders")]
public IReadOnlyDictionary<string, string> Headers { get; set; }
```

---

### `ConfigNameAttribute`

Maps a property to an INI key whose name differs from the C# property name.

| Property | Type | Description |
|---|---|---|
| `Name` | `string` | The exact key name as it appears in the INI file. |

```csharp
// INI: sServer=PRODSERVER
[ConfigName("sServer")]
public string Server { get; set; }
```

---

### `DefaultValueProviderAttribute`

Specifies a static no-parameter method that returns the property's default value at runtime.  
Use when the default cannot be expressed as a compile-time constant (e.g., `CultureInfo.InvariantCulture`).

| Property | Type | Description |
|---|---|---|
| `ProviderType` | `Type` | The class containing the static method. |
| `MethodName` | `string` | Name of the public or non-public static method with no parameters. |

`GetDefaultValue()` invokes the method via reflection and returns the result.

```csharp
[DefaultValueProvider(typeof(MyDefaults), nameof(MyDefaults.GetDefaultTimeout))]
public TimeSpan Timeout { get; set; }

// In MyDefaults:
public static TimeSpan GetDefaultTimeout() => TimeSpan.FromSeconds(30);
```

---

### `DoNotPersistAttribute`

Prevents a property from being written to the INI file during `Save()`. The property is still loaded from the file on startup.

```csharp
// Read from INI but never persisted back
[DoNotPersist]
public string ComputedValue { get; set; }
```

---

### `IgnoreAttribute` (extends `DoNotPersistAttribute`)

Prevents a property from being both loaded and saved. The property is completely invisible to the configuration system.

```csharp
// Neither loaded nor saved
[Ignore]
public string RuntimeOnlyState { get; set; }
```

---

### `EncryptedAttribute`

Marks a property as encrypted. `ConfigSection<T>` automatically decrypts the value on load using `Encryptor.Default` and encrypts it on save.

Applicable to `string` properties and `Dictionary<string,string>` properties (all values encrypted).

```csharp
[Encrypted]
public string ApiKey { get; set; }

[Dictionary, Encrypted]
public Dictionary<string, string> Credentials { get; set; }
```

---

## Notes

- `System.ComponentModel.DefaultValueAttribute` is also recognised by `DefaultValueHelper` and takes precedence over `DefaultValueProviderAttribute` when both are present.
- Properties decorated with `[Ignore]` are excluded during `ConfigSection<T>` construction and never touched.
- Properties decorated with `[DoNotPersist]` are included during load but excluded from `SaveToConfiguration`.
