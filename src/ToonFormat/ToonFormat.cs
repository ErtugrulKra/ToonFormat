namespace ToonFormat
{
    /// <summary>
    /// Token-Oriented Object Notation (TOON) - A compact, human-readable format for LLM prompts.
    /// Reduces token usage by 30-60% compared to JSON.
    /// </summary>
    public static class ToonFormat
    {
        /// <summary>
        /// Encodes a C# object to TOON format string.
        /// </summary>
        /// <param name="obj">The object to encode (dictionary, list, or primitive value)</param>
        /// <param name="indent">Number of spaces per indentation level (default: 2)</param>
        /// <param name="delimiter">Field delimiter for tabular arrays (default: ",")</param>
        /// <returns>TOON-formatted string</returns>
        public static string Encode(object? obj, int indent = 2, string delimiter = ",")
        {
            return Encoder.Encode(obj, indent, delimiter);
        }

        /// <summary>
        /// Decodes a TOON format string to C# object.
        /// </summary>
        /// <param name="toonString">TOON-formatted string</param>
        /// <param name="indent">Expected indentation level (default: 2)</param>
        /// <param name="strict">Enable strict validation (default: true)</param>
        /// <returns>Decoded object (dictionary, list, or primitive value)</returns>
        public static object? Decode(string toonString, int indent = 2, bool strict = true)
        {
            return Decoder.Decode(toonString, indent, strict);
        }

        /// <summary>
        /// Loads TOON data from a file.
        /// </summary>
        /// <param name="filePath">Path to the TOON file</param>
        /// <param name="indent">Expected indentation level (default: 2)</param>
        /// <param name="strict">Enable strict validation (default: true)</param>
        /// <returns>Decoded object</returns>
        public static object? Load(string filePath, int indent = 2, bool strict = true)
        {
            var content = System.IO.File.ReadAllText(filePath);
            return Decode(content, indent, strict);
        }

        /// <summary>
        /// Saves an object to a TOON file.
        /// </summary>
        /// <param name="obj">The object to save</param>
        /// <param name="filePath">Path to save the TOON file</param>
        /// <param name="indent">Indentation level (default: 2)</param>
        /// <param name="delimiter">Field delimiter (default: ",")</param>
        public static void Save(object? obj, string filePath, int indent = 2, string delimiter = ",")
        {
            var content = Encode(obj, indent, delimiter);
            System.IO.File.WriteAllText(filePath, content);
        }

        /// <summary>
        /// Compares JSON and TOON representations of the same data.
        /// </summary>
        /// <param name="obj">The object to compare</param>
        /// <param name="jsonIndent">JSON indentation level for comparison</param>
        /// <returns>Comparison metrics</returns>
        public static ComparisonMetrics CompareSizes(object? obj, int jsonIndent = 2)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(obj, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = jsonIndent > 0,
                IndentCharacter = ' ',
                IndentSize = jsonIndent
            });
            var toon = Encode(obj);

            var jsonSize = json.Length;
            var toonSize = toon.Length;
            var jsonTokens = EstimateTokens(json);
            var toonTokens = EstimateTokens(toon);

            return new ComparisonMetrics
            {
                JsonSize = jsonSize,
                ToonSize = toonSize,
                JsonTokens = jsonTokens,
                ToonTokens = toonTokens,
                SizeReduction = jsonSize > 0 ? ((jsonSize - toonSize) / (double)jsonSize * 100) : 0,
                TokenReduction = jsonTokens > 0 ? ((jsonTokens - toonTokens) / (double)jsonTokens * 100) : 0
            };
        }

        private static int EstimateTokens(string text)
        {
            // Simple approximation: ~4 characters per token
            return text.Length / 4 + 1;
        }
    }

    /// <summary>
    /// Comparison metrics between JSON and TOON formats.
    /// </summary>
    public class ComparisonMetrics
    {
        /// <summary>
        /// Gets or sets the size of the JSON representation in bytes.
        /// </summary>
        public int JsonSize { get; set; }

        /// <summary>
        /// Gets or sets the size of the TOON representation in bytes.
        /// </summary>
        public int ToonSize { get; set; }

        /// <summary>
        /// Gets or sets the number of tokens in the JSON representation.
        /// </summary>
        public int JsonTokens { get; set; }

        /// <summary>
        /// Gets or sets the number of tokens in the TOON representation.
        /// </summary>
        public int ToonTokens { get; set; }

        /// <summary>
        /// Gets or sets the size reduction percentage (0-100).
        /// </summary>
        public double SizeReduction { get; set; }

        /// <summary>
        /// Gets or sets the token reduction percentage (0-100).
        /// </summary>
        public double TokenReduction { get; set; }
    }
}

