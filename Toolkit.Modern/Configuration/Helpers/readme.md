# Configuration / Helpers

This folder contains internal helper types used by `ConfigSection<T>` to parse and populate typed configuration properties. These types are not part of the public API.

---

## Types

### `IParsingHelper` (internal interface)

Defines the contract for parsing a string value and applying the result to an object or property via reflection.

| Method | Description |
|---|---|
| `ParseAndPopulateObject(Type? type, object? target, string? value)` | Parse `value` into an instance of `type`, then shallow-copy its writable public properties onto `target`. No-ops when `value` is null or whitespace. |
| `ParseAndPopulateProperty(PropertyInfo prop, object owner, string value)` | Parse `value` and assign the result to `prop` on `owner`. Applies the property's default value when `value` is null or whitespace. |

Implementations are expected to:
- Unwrap `Nullable<T>` types before parsing.
- Populate only publicly readable and writable instance members.
- Apply defensive null checks.

---

### `ParsingHelper` (internal class)

Concrete implementation of `IParsingHelper`. Delegates actual type parsing to an `IParser` instance (defaults to `Parser.Default` from `ByteForge.Toolkit.Utilities`).

| Member | Description |
|---|---|
| `Default` (static) | Shared instance backed by `Parser.Default`. Used throughout `ConfigSection<T>`. |
| `ParseAndPopulateProperty(PropertyInfo, object, string)` (static) | Convenience static method that delegates to `Default`. |
| `ParseAndPopulateObject(Type, object, string)` (static) | Convenience static method that delegates to `Default`. |

**Behaviour of `ParseAndPopulateProperty`:**
1. Resolves the property's default value via `DefaultValueHelper.ResolveDefaultValue`.
2. If `value` is null or whitespace, sets the property to the default and returns.
3. Otherwise, resolves the underlying type (unwrapping nullable), calls `ParseAndPopulateObject` to populate a temp instance, then sets the property.

**Behaviour of `ParseAndPopulateObject`:**
1. Unwraps nullable types.
2. Calls `parser.Parse(type, value)` to obtain a parsed instance.
3. Iterates public readable+writable properties of the parsed instance and copies non-null values to `target`.

---

### `DefaultValueHelper` (internal static class)

Resolves default values for properties during loading, in the following priority order:

1. `System.ComponentModel.DefaultValueAttribute` — compile-time constant default.
2. `DefaultValueProviderAttribute` — runtime default via a static factory method.
3. Type default — `null` for reference types and nullable value types; `Activator.CreateInstance` for value types.

| Method | Description |
|---|---|
| `ResolveDefaultValue(PropertyInfo prop)` | Returns the resolved default value, or throws `InvalidOperationException` for non-nullable value types with no configured default. |

Throws `InvalidOperationException` if a configured default value cannot be converted to the property's declared type.

---

## Notes

- All types in this folder are `internal` and are not accessible from outside the `ByteForge.Toolkit.Configuration` assembly.
- `ParsingHelper.Default` is instantiated once and reused to avoid repeated allocation.
- The shallow-copy strategy in `ParseAndPopulateObject` means that only top-level properties are merged; nested objects are replaced entirely if non-null.
