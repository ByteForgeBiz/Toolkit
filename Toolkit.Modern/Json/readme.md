# Json Module

## Overview

The **Json** module provides delta-based JSON serialisation built on top of Newtonsoft.Json (Json.NET). Instead of serialising an entire object graph, it compares an object against a baseline ("default") object and emits only the properties whose values differ. This dramatically reduces payload size for patch-style API responses and real-time sync scenarios.

---

## Key Types

| Type | Kind | Description |
|------|------|-------------|
| `JsonDeltaSerializer` | Static class | Serialises only the properties of an object that differ from a provided default |
| `DeltaContractResolver` | Class (extends `DefaultContractResolver`) | Newtonsoft.Json contract resolver that filters properties at serialisation time |

Both types live in the `ByteForge.Toolkit.Json` namespace.

---

## JsonDeltaSerializer

A single static method performs the delta serialisation.

```csharp
public static string SerializeDelta<T>(T currentObject, T defaultObject)
```

| Parameter | Description |
|-----------|-------------|
| `currentObject` | The object to serialise |
| `defaultObject` | The baseline to compare against. Only properties where `!Equals(currentValue, defaultValue)` are included in the output. If `null`, the entire object is serialised. |

**Returns:** A JSON string containing only the properties whose values differ from `defaultObject`. Properties that are equal to their defaults are omitted entirely.

**Null handling:** `NullValueHandling.Include` is used so that a property explicitly set to `null` (when the default was non-null) is still emitted.

---

## DeltaContractResolver

`DeltaContractResolver` extends Newtonsoft.Json's `DefaultContractResolver`. It accepts a default object in its constructor and overrides `CreateProperty` to attach a `ShouldSerialize` predicate to every JSON property. The predicate compares the current instance's property value against the default object's value using `Equals`; if they match, the property is suppressed.

If the comparison throws (e.g. due to a property with no getter), the property is included conservatively.

You can use `DeltaContractResolver` directly when you need finer control over serialisation settings:

```csharp
var resolver = new DeltaContractResolver(defaultObject);

var settings = new JsonSerializerSettings
{
    ContractResolver  = resolver,
    NullValueHandling = NullValueHandling.Include,
    Formatting        = Formatting.None,
};

string json = JsonConvert.SerializeObject(currentObject, settings);
```

---

## Usage

### Basic delta serialisation

```csharp
var original = new Product { Id = 1, Name = "Widget", Price = 19.99m, Stock = 100 };

// Clone and modify
var updated = new Product { Id = 1, Name = "Widget", Price = 24.99m, Stock = 100 };

string delta = JsonDeltaSerializer.SerializeDelta(updated, original);
// → {"Price":24.99}
// Id, Name, and Stock are omitted because they are unchanged.
```

### Sending only changes to an API

```csharp
public string BuildPatchPayload(Order before, Order after)
{
    return JsonDeltaSerializer.SerializeDelta(after, before);
}

// Full order JSON might be 2 KB.
// If only Status changed, the delta is a few bytes: {"Status":"Shipped"}
```

### Default-object comparison

If `defaultObject` is `null`, `SerializeDelta` falls back to a full `JsonConvert.SerializeObject` call:

```csharp
// Equivalent to JsonConvert.SerializeObject(current)
string full = JsonDeltaSerializer.SerializeDelta(current, null);
```

### Custom serialiser settings

When you need additional control (date formats, converters, indentation):

```csharp
var resolver = new DeltaContractResolver(baseline);

var settings = new JsonSerializerSettings
{
    ContractResolver  = resolver,
    NullValueHandling = NullValueHandling.Include,
    DateFormatString  = "yyyy-MM-ddTHH:mm:ss",
    Formatting        = Formatting.Indented,
};

string delta = JsonConvert.SerializeObject(current, settings);
```

---

## How it works

1. `JsonDeltaSerializer.SerializeDelta<T>` constructs a `DeltaContractResolver` seeded with `defaultObject`.
2. `DeltaContractResolver.CreateProperty` overrides each JSON property's `ShouldSerialize` predicate to compare values via `Equals`.
3. `JsonConvert.SerializeObject` is called; properties where `ShouldSerialize` returns `false` (value unchanged) are omitted from the output.

The comparison is shallow — only the property value itself is compared with `Equals`. For reference types this means reference equality unless the type overrides `Equals`.

---

## Limitations

| Limitation | Notes |
|-----------|-------|
| Shallow comparison | Reference types that do not override `Equals` will be serialised whenever the reference differs, even if the content is the same |
| No deserialization merge | The module provides serialisation only; merging a delta back onto a baseline object must be handled by the caller |
| Collections | List/array changes are detected as a whole (reference or element-level `Equals`), not as individual additions/removals |
| Circular references | Not supported; configure `JsonSerializerSettings.ReferenceLoopHandling` in the caller if needed |

---

## 📖 Documentation Links

| Module | Description |
|--------|-------------|
| **[CLI](../CommandLine/readme.md)** | Command-line parsing |
| **[Configuration](../Configuration/readme.md)** | INI-based configuration |
| **[Core](../Core/readme.md)** | Core utilities |
| **[Data](../Data/readme.md)** | Database and file processing |
| **[DataStructures](../DataStructures/readme.md)** | Collections and URL utilities |
| **[Logging](../Logging/readme.md)** | Structured logging |
| **[Mail](../Mail/readme.md)** | Email processing |
| **[Net](../Net/readme.md)** | Network file transfers |
| **[Security](../Security/readme.md)** | Encryption and security |
| **[Utils](../Utilities/readme.md)** | General utilities |
