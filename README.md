# ToonFormat

C# implementation of **Token-Oriented Object Notation (TOON)** – A compact, human-readable, schema-aware JSON format designed for LLM prompts.

TOON reduces token usage by **30-60%** compared to JSON by eliminating redundant punctuation and using a tabular format for uniform data structures.

## Features

- **Token Efficient**: 30-60% fewer tokens than JSON
- **Tabular Format**: Optimized for arrays of uniform objects
- **Round-trip Safe**: Lossless encoding/decoding
- **.NET Native**: Simple API similar to JSON serialization
- **Human Readable**: Easy to read and debug
- **Cross-platform**: Supports .NET Standard 2.0+

## Installation

Install from NuGet Package Manager:

```bash
Install-Package ToonFormat
```

Or using .NET CLI:

```bash
dotnet add package ToonFormat
```

## Global Tool (CLI)

Install the global tool for command-line usage:

```bash
dotnet tool install -g ToonFormat.Tool
```

After installation, use the `toon` command:

```bash
# Convert JSON to TOON
toon encode input.json
toon encode input.json -o output.toon

# Convert TOON to JSON
toon decode input.toon
toon decode input.toon -o output.json

# Read from stdin
echo '{"key": "value"}' | toon encode
cat data.toon | toon decode
```

## Quick Start

```csharp
using ToonFormat;

// Encode C# objects to TOON
var data = new Dictionary<string, object>
{
    ["products"] = new List<Dictionary<string, object>>
    {
        new Dictionary<string, object> { ["sku"] = "A123", ["name"] = "Widget", ["price"] = 9.99 },
        new Dictionary<string, object> { ["sku"] = "B456", ["name"] = "Gadget", ["price"] = 19.99 }
    }
};

var toon = ToonFormat.Encode(data);
Console.WriteLine(toon);
// products[2]{sku,name,price}:
//   A123,Widget,9.99
//   B456,Gadget,19.99

// Decode TOON back to C# objects
var decoded = ToonFormat.Decode(toon);
```

## Usage

### Encoding

```csharp
using ToonFormat;

// Simple object
var simple = new Dictionary<string, object>
{
    ["id"] = 1,
    ["name"] = "Alice"
};
var toon = ToonFormat.Encode(simple);
// id: 1
// name: Alice

// Nested object
var nested = new Dictionary<string, object>
{
    ["user"] = new Dictionary<string, object>
    {
        ["id"] = 1,
        ["name"] = "Alice"
    }
};
var toonNested = ToonFormat.Encode(nested);
// user:
//   id: 1
//   name: Alice

// Tabular array (uniform objects)
var items = new Dictionary<string, object>
{
    ["items"] = new List<Dictionary<string, object>>
    {
        new Dictionary<string, object> { ["sku"] = "A1", ["qty"] = 2 },
        new Dictionary<string, object> { ["sku"] = "B2", ["qty"] = 1 }
    }
};
var toonItems = ToonFormat.Encode(items);
// items[2]{sku,qty}:
//   A1,2
//   B2,1

// Custom delimiter
var toonTab = ToonFormat.Encode(items, delimiter: "\t");
```

### Decoding

```csharp
using ToonFormat;

var toon = @"
products[2]{sku,name,price}:
  A123,Widget,9.99
  B456,Gadget,19.99
";

var data = ToonFormat.Decode(toon);
// Returns Dictionary<string, object> with products array
```

### File I/O

```csharp
using ToonFormat;

// Load from file
var data = ToonFormat.Load("data.toon");

// Save to file
ToonFormat.Save(data, "output.toon");
```

### Token Comparison

```csharp
using ToonFormat;

var data = new Dictionary<string, object>
{
    ["products"] = new List<Dictionary<string, object>>
    {
        new Dictionary<string, object> { ["id"] = 1, ["name"] = "Product 1" },
        new Dictionary<string, object> { ["id"] = 2, ["name"] = "Product 2" }
    }
};

var metrics = ToonFormat.CompareSizes(data);
Console.WriteLine($"Token reduction: {metrics.TokenReduction:F1}%");
Console.WriteLine($"Size reduction: {metrics.SizeReduction:F1}%");
```

## API Reference

### `ToonFormat.Encode(object? obj, int indent = 2, string delimiter = ",")`

Converts C# objects to TOON format.

**Parameters:**
- `obj`: C# object (Dictionary, List, or primitive)
- `indent`: Number of spaces per indentation level (default: 2)
- `delimiter`: Field delimiter for tabular arrays (default: ",")

**Returns:** TOON-formatted string

### `ToonFormat.Decode(string toonString, int indent = 2, bool strict = true)`

Converts TOON-formatted string to C# objects.

**Parameters:**
- `toonString`: TOON-formatted string
- `indent`: Expected indentation level (default: 2)
- `strict`: Enable strict validation (default: true)

**Returns:** C# object (Dictionary, List, or primitive)

### `ToonFormat.Load(string filePath, int indent = 2, bool strict = true)`

Loads TOON data from a file.

### `ToonFormat.Save(object? obj, string filePath, int indent = 2, string delimiter = ",")`

Saves an object to a TOON file.

### `ToonFormat.CompareSizes(object? obj, int jsonIndent = 2)`

Compares JSON and TOON representations.

**Returns:** `ComparisonMetrics` object with:
- `JsonSize`: JSON string length
- `ToonSize`: TOON string length
- `JsonTokens`: Approximate JSON token count
- `ToonTokens`: Approximate TOON token count
- `SizeReduction`: Percentage reduction in size
- `TokenReduction`: Percentage reduction in tokens

## Format Examples

### Object
```csharp
new Dictionary<string, object> { ["id"] = 1, ["name"] = "Ada" }
// →
// id: 1
// name: Ada
```

### Nested Object
```csharp
new Dictionary<string, object>
{
    ["user"] = new Dictionary<string, object> { ["id"] = 1 }
}
// →
// user:
//   id: 1
```

### Tabular Array (Uniform Objects)
```csharp
new Dictionary<string, object>
{
    ["items"] = new List<Dictionary<string, object>>
    {
        new Dictionary<string, object> { ["id"] = 1, ["qty"] = 5 },
        new Dictionary<string, object> { ["id"] = 2, ["qty"] = 3 }
    }
}
// →
// items[2]{id,qty}:
//   1,5
//   2,3
```

## When to Use TOON

**TOON excels at:**
- Uniform arrays of objects (same fields, primitive values)
- Large datasets with consistent structure
- LLM prompts where token efficiency matters

**JSON is better for:**
- Non-uniform data
- Deeply nested structures
- Objects with varying field sets
- API responses and storage

## Token Savings

TOON achieves significant token savings, especially for tabular data:

**JSON:** ~45 tokens
```json
{
  "products": [
    {"sku": "A123", "name": "Widget", "price": 9.99},
    {"sku": "B456", "name": "Gadget", "price": 19.99}
  ]
}
```

**TOON:** ~19 tokens (58% reduction)
```
products[2]{sku,name,price}:
  A123,Widget,9.99
  B456,Gadget,19.99
```

## Requirements

- .NET Standard 2.0 or higher
- .NET Core 2.0+ or .NET Framework 4.6.1+

## License

MIT License

## Credits

- Based on [TOON format](https://github.com/toon-format/toon) by Johann Schopplich
- C# implementation by Ertugrul Kara

## Links

- [NuGet Package](https://www.nuget.org/packages/ToonFormat)
- [GitHub Repository](https://github.com/ErtugrulKra/ToonFormat)
- [TOON Specification](https://github.com/toon-format/toon)
- [Format Documentation](https://toonformat.dev)

