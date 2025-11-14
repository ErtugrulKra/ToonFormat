using System;
using System.Collections.Generic;

namespace ToonFormat.Examples
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== ToonFormat Examples ===\n");

            // Example 1: Simple object
            Example1_SimpleObject();

            // Example 2: Tabular array
            Example2_TabularArray();

            // Example 3: Nested object
            Example3_NestedObject();

            // Example 4: Round-trip
            Example4_RoundTrip();

            // Example 5: Token comparison
            Example5_TokenComparison();
        }

        static void Example1_SimpleObject()
        {
            Console.WriteLine("Example 1: Simple Object");
            var data = new Dictionary<string, object>
            {
                ["id"] = 1,
                ["name"] = "Alice",
                ["active"] = true
            };

            var toon = ToonFormat.Encode(data);
            Console.WriteLine("TOON:");
            Console.WriteLine(toon);
            Console.WriteLine();
        }

        static void Example2_TabularArray()
        {
            Console.WriteLine("Example 2: Tabular Array");
            var data = new Dictionary<string, object>
            {
                ["products"] = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object> { ["sku"] = "A123", ["name"] = "Widget", ["price"] = 9.99 },
                    new Dictionary<string, object> { ["sku"] = "B456", ["name"] = "Gadget", ["price"] = 19.99 },
                    new Dictionary<string, object> { ["sku"] = "C789", ["name"] = "Thingy", ["price"] = 5.99 }
                }
            };

            var toon = ToonFormat.Encode(data);
            Console.WriteLine("TOON:");
            Console.WriteLine(toon);
            Console.WriteLine();
        }

        static void Example3_NestedObject()
        {
            Console.WriteLine("Example 3: Nested Object");
            var data = new Dictionary<string, object>
            {
                ["user"] = new Dictionary<string, object>
                {
                    ["id"] = 1,
                    ["name"] = "Alice",
                    ["preferences"] = new Dictionary<string, object>
                    {
                        ["theme"] = "dark",
                        ["language"] = "en"
                    }
                }
            };

            var toon = ToonFormat.Encode(data);
            Console.WriteLine("TOON:");
            Console.WriteLine(toon);
            Console.WriteLine();
        }

        static void Example4_RoundTrip()
        {
            Console.WriteLine("Example 4: Round-trip Encoding/Decoding");
            var original = new Dictionary<string, object>
            {
                ["items"] = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object> { ["id"] = 1, ["name"] = "Item 1", ["qty"] = 5 },
                    new Dictionary<string, object> { ["id"] = 2, ["name"] = "Item 2", ["qty"] = 3 }
                }
            };

            var encoded = ToonFormat.Encode(original);
            Console.WriteLine("Encoded:");
            Console.WriteLine(encoded);
            Console.WriteLine();

            var decoded = ToonFormat.Decode(encoded);
            Console.WriteLine("Decoded successfully: " + (decoded != null));
            Console.WriteLine();
        }

        static void Example5_TokenComparison()
        {
            Console.WriteLine("Example 5: Token Comparison");
            var data = new Dictionary<string, object>
            {
                ["products"] = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object> { ["id"] = 1, ["name"] = "Product 1", ["price"] = 10.0 },
                    new Dictionary<string, object> { ["id"] = 2, ["name"] = "Product 2", ["price"] = 20.0 }
                }
            };

            var metrics = ToonFormat.CompareSizes(data);
            Console.WriteLine($"JSON size: {metrics.JsonSize} bytes");
            Console.WriteLine($"TOON size: {metrics.ToonSize} bytes");
            Console.WriteLine($"Size reduction: {metrics.SizeReduction:F1}%");
            Console.WriteLine($"Token reduction: {metrics.TokenReduction:F1}%");
            Console.WriteLine();
        }
    }
}

