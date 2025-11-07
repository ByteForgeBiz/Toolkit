# Json Module

## Overview

The **Json** module provides advanced JSON serialization capabilities, particularly delta (difference-based) serialization that reduces payload size by transmitting only changed properties. It integrates with Newtonsoft.Json (Json.NET) for customizable serialization behavior.

---

## Purpose

Modern applications need:
1. **Efficient transmission** - Reduce bandwidth for data sync
2. **Change tracking** - Serialize only modified properties
3. **API optimization** - Smaller response payloads
4. **Incremental updates** - Send deltas instead of full objects
5. **Custom formatting** - Control JSON output format

---

## Key Classes

### `JsonDeltaSerializer`
**Purpose:** Serializes only changed properties to reduce payload size.

**Concept:**
- Compare object with original state
- Identify changed properties
- Include only changed properties in JSON
- Significantly reduce bandwidth for large objects

**Key Methods:**
```csharp
// Serialize only changes (delta)
string SerializeDelta<T>(T currentState, T originalState);

// Serialize with change tracking
string SerializeDelta<T>(T obj, IChangeTracker tracker);

// Get list of changed properties
IList<string> GetChangedProperties<T>(T current, T original);

// Deserialize delta and merge with original
T DeserializeDelta<T>(T original, string json);
```

**Usage:**
```csharp
// Initial state
var customer = new Customer { Id = 1, Name = "John", Email = "john@example.com" };
var json = JsonConvert.SerializeObject(customer);  // Full object

// Later, object is modified
customer.Name = "John Doe";  // Only name changed

// Serialize delta (only changes)
var originalCustomer = new Customer { Id = 1, Name = "John", Email = "john@example.com" };
var deltaJson = JsonDeltaSerializer.SerializeDelta(customer, originalCustomer);
// Result: { "Name": "John Doe" }  (much smaller!)

// Send only delta to server/client
SendToRemote(deltaJson);  // ~50 bytes instead of ~200 bytes
```

### `DeltaContractResolver`
**Purpose:** Custom contract resolver for Newtonsoft.Json enabling delta serialization.

**Responsibilities:**
- Intercept serialization process
- Filter properties based on change status
- Customize property inclusion logic
- Work with change trackers

**Key Methods:**
```csharp
protected override List<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization);

void SetOriginalObject<T>(T original);
void SetChangeTracker(IChangeTracker tracker);
```

**Implementation Details:**
```csharp
var settings = new JsonSerializerSettings
{
    ContractResolver = new DeltaContractResolver()
};

// Configure resolver with original object
((DeltaContractResolver)settings.ContractResolver).SetOriginalObject(original);

var json = JsonConvert.SerializeObject(modified, settings);
```

---

## Usage Patterns

### Basic Delta Serialization

```csharp
public class Product
{
    public int Id { get; set; }
  public string Name { get; set; }
    public decimal Price { get; set; }
    public string Description { get; set; }
    public DateTime LastModified { get; set; }
}

// Original product
var product = new Product
{
    Id = 123,
    Name = "Widget",
    Price = 19.99m,
    Description = "A useful widget",
    LastModified = DateTime.Now
};

// Save original state
var originalJson = JsonConvert.SerializeObject(product);

// User modifies price only
product.Price = 24.99m;
product.LastModified = DateTime.Now;

// Serialize only changes
var deltaJson = JsonDeltaSerializer.SerializeDelta(product, originalProduct);
// Result: { "Price": 24.99, "LastModified": "2024-01-15..." }

// Original full size: ~250 bytes
// Delta size: ~60 bytes (76% smaller!)
```

### API Response Optimization

```csharp
public class OrderService
{
    public OrderResponse GetOrder(int orderId)
    {
        var order = _database.GetOrder(orderId);
        return new OrderResponse { Order = order };
    }
    
    public OrderResponse UpdateOrder(int orderId, Order updates)
    {
        var existing = _database.GetOrder(orderId);
        
        // Apply updates
        existing.Status = updates.Status;
        existing.Total = updates.Total;
        
        _database.Save(existing);
        
      // Return only changed properties
        var serializer = new JsonDeltaSerializer();
   var original = _database.GetOrderHistory(orderId).Last();
        
  var deltaJson = serializer.SerializeDelta(existing, original);
 
        return new OrderResponse 
      { 
            Data = deltaJson,
            IsPartial = true
        };
    }
}

// Usage:
// POST /orders/123
// Response:  { "Status": "Shipped", "Total": 150.00, "LastModified": "..." }
// Instead of: { all 50 properties of order... }
```

### Real-Time Synchronization

```csharp
public class DataSync
{
  private Dictionary<int, Customer> _cache = new();
    private JsonDeltaSerializer _serializer = new();
    
 public void HandleServerUpdate(int customerId, string deltaJson)
    {
        if (!_cache.TryGetValue(customerId, out var customer))
        {
 // First time seeing this customer
   customer = JsonConvert.DeserializeObject<Customer>(deltaJson);
 _cache[customerId] = customer;
        return;
        }
        
     // Merge delta with cached version
      var updated = _serializer.DeserializeDelta(customer, deltaJson);
        _cache[customerId] = updated;
        
        UI.RefreshCustomerDisplay(updated);
    }
}
```

### Change Tracking Implementation

```csharp
public interface IChangeTracker
{
    bool HasChanged(string propertyName);
    IEnumerable<string> GetChangedProperties();
}

public class Customer : IChangeTracker
{
    private HashSet<string> _changedProperties = new();
    
    private string _name;
    public string Name
    {
        get => _name;
        set
   {
            if (_name != value)
            {
        _name = value;
     _changedProperties.Add(nameof(Name));
   }
        }
    }
    
    public bool HasChanged(string propertyName) 
        => _changedProperties.Contains(propertyName);
    
public IEnumerable<string> GetChangedProperties() 
        => _changedProperties;
}

// Usage with tracker
var customer = new Customer { Name = "Old" };
customer.Name = "New";

var json = JsonDeltaSerializer.SerializeDelta(customer, customer as IChangeTracker);
```

### Nested Object Deltas

```csharp
public class Order
{
    public int Id { get; set; }
    public Customer Customer { get; set; }
    public List<OrderLine> Items { get; set; }
}

public class Customer
{
    public string Name { get; set; }
    public string Address { get; set; }
}

// Original
var original = new Order
{
    Id = 1,
    Customer = new Customer { Name = "John", Address = "123 Main St" },
    Items = new List<OrderLine> { /* ... */ }
};

// Modified
var modified = new Order
{
    Id = 1,
    Customer = new Customer { Name = "John", Address = "456 Oak Ave" },  // Address changed
    Items = new List<OrderLine> { /* ... */ }
};

// Delta includes nested change
var delta = JsonDeltaSerializer.SerializeDelta(modified, original);
// Result: { "Customer": { "Address": "456 Oak Ave" } }
```

---

## Configuration

### Custom Serialization Settings

```csharp
// Create resolver
var resolver = new DeltaContractResolver();
resolver.SetOriginalObject(originalObject);

// Configure settings
var settings = new JsonSerializerSettings
{
    ContractResolver = resolver,
    NullValueHandling = NullValueHandling.Ignore,
    DateFormatString = "yyyy-MM-ddTHH:mm:ss",
    Formatting = Formatting.None  // Compact JSON
};

var json = JsonConvert.SerializeObject(obj, settings);
```

### Ignore Properties

```csharp
public class Customer
{
    public int Id { get; set; }
    
    public string Name { get; set; }
    
    [JsonIgnore]
    public string InternalNotes { get; set; }  // Never serialized
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string MiddleName { get; set; }  // Ignored if null
}
```

---

## Performance Benefits

### Bandwidth Reduction

```
Scenario: Order with 50 properties
- Full JSON: ~2 KB
- Delta (5 changed): ~200 bytes (90% reduction!)
- Multiple deltas: ~100 bytes each (95% reduction!)
```

### Response Time

```
// Before (full serialization)
Serialize 1000 objects: ~50ms
Network transmission: ~2 MB (2000ms at 1Mbps)
Total: 2050ms

// After (delta serialization)
Serialize 1000 deltas: ~5ms
Network transmission: ~200 KB (200ms at 1Mbps)
Total: 205ms (10x faster!)
```

### Network Usage

```
Monthly impact:
- Full API responses: 100 GB/month
- With delta: 5 GB/month
- Savings: 95 GB = significant cost reduction
```

---

## Limitations & Considerations

### Complexity
- Requires tracking original state
- More complex than simple serialization
- Overhead for very small objects

### Versioning
- Schema changes require careful handling
- Client must understand delta format
- Version negotiation needed

### Nested Objects
- Nested arrays difficult to delta
- Circular references problematic
- Complex graphs require careful design

---

## File Organization

### `JsonDeltaSerializer.cs`
Main delta serializer class.

**Contains:**
- Serialization logic
- Change detection
- Delta comparison
- Deserialization merge

### `DeltaContractResolver.cs`
Custom Json.NET contract resolver.

**Contains:**
- Property filtering logic
- Change tracker integration
- Original object comparison

---

## Integration Example

```csharp
public class ApiController : ControllerBase
{
    private readonly IRepository _repo;

    // GET /api/customers/123
    [HttpGet("{id}")]
    public ActionResult<string> GetCustomer(int id)
    {
        var customer = _repo.GetCustomer(id);
 var json = JsonConvert.SerializeObject(customer);
    
      // Cache for next update
    HttpContext.Session.SetString($"customer_{id}", json);
   
   return json;
    }
  
    // PATCH /api/customers/123
    [HttpPatch("{id}")]
    public ActionResult<string> UpdateCustomer(int id, [FromBody] Customer updates)
    {
  var original = _repo.GetCustomer(id);
        var existing = JsonConvert.DeserializeObject<Customer>(
            HttpContext.Session.GetString($"customer_{id}")
    );
        
        // Apply updates
        foreach (var prop in updates.GetType().GetProperties())
        {
            var newValue = prop.GetValue(updates);
          if (newValue != null)
  prop.SetValue(original, newValue);
 }
        
   _repo.Save(original);
        
        // Return delta
     var deltaJson = new JsonDeltaSerializer()
            .SerializeDelta(original, existing);
     
        return deltaJson;
    }
}
```

---

## Best Practices

### 1. **Cache Original State**
```csharp
// Good - keep reference to original
var original = customer.Clone();
customer.Name = "NewName";
var delta = serializer.SerializeDelta(customer, original);

// Bad - original lost
customer.Name = "NewName";
var delta = serializer.SerializeDelta(customer, null);  // Can't compute delta
```

### 2. **Versioning**
```csharp
public class DeltaResponse
{
    public int Version { get; set; } = 2;
    public string Data { get; set; }
    public bool IsPartial { get; set; }
}

// Include version so client knows how to interpret delta
```

### 3. **Fallback to Full**
```csharp
// If delta becomes too complex, fall back to full serialization
if (changedPropertiesCount > 20 || complexNestedChanges)
{
    return JsonConvert.SerializeObject(current);  // Full object
}
else
{
  return JsonDeltaSerializer.SerializeDelta(current, original);  // Delta
}
```

---

## Summary

The Json module provides:

**Key Strengths:**
- ? Dramatic bandwidth reduction (80-95%)
- ? Faster transmission
- ? Efficient incremental updates
- ? Seamless integration with Json.NET
- ? Nested object support
- ? Change tracker integration

**Best For:**
- Real-time data synchronization
- Mobile/bandwidth-constrained networks
- High-frequency API updates
- Large object transmission
- IoT and edge scenarios

**Not Ideal For:**
- Simple, one-off requests
- Stateless APIs
- Small objects (overhead outweighs benefit)
- Offline-first applications (complex merging)

