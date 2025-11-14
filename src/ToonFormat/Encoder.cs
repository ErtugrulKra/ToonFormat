using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace ToonFormat
{
    internal static class Encoder
    {
        public static string Encode(object? value, int indent, string delimiter)
        {
            return EncodeValue(value, indent, delimiter, 0);
        }

        private static string EncodeValue(object? value, int indent, string delimiter, int level)
        {
            if (value == null)
                return "null";

            if (value is bool b)
                return b ? "true" : "false";

            if (value is int || value is long || value is short || value is byte)
                return value.ToString() ?? "0";

            if (value is float || value is double || value is decimal)
                return value.ToString() ?? "0";

            if (value is string s)
                return EncodeString(s);

            if (value is IDictionary dict)
                return EncodeDictionary(dict, indent, delimiter, level);

            if (value is IEnumerable enumerable && !(value is string))
                return EncodeArray(enumerable.Cast<object?>().ToList(), indent, delimiter, level);

            // Fallback to JSON string representation
            return JsonSerializer.Serialize(value);
        }

        private static string EncodeString(string s)
        {
            bool needsQuotes = string.IsNullOrEmpty(s) ||
                              char.IsWhiteSpace(s[0]) ||
                              char.IsWhiteSpace(s[s.Length - 1]) ||
                              s.Contains(",") ||
                              s.Contains(":") ||
                              s.Contains("[") ||
                              s.Contains("]") ||
                              s.Contains("{") ||
                              s.Contains("}") ||
                              s.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                              s.Equals("false", StringComparison.OrdinalIgnoreCase) ||
                              s.Equals("null", StringComparison.OrdinalIgnoreCase) ||
                              LooksLikeNumber(s);

            if (!needsQuotes)
                return s;

            var escaped = s.Replace("\\", "\\\\").Replace("\"", "\\\"");
            return $"\"{escaped}\"";
        }

        private static bool LooksLikeNumber(string s)
        {
            return double.TryParse(s, out _);
        }

        private static string EncodeDictionary(IDictionary dict, int indent, string delimiter, int level)
        {
            if (dict.Count == 0)
                return "";

            var lines = new List<string>();
            var prefix = new string(' ', indent * level);

            foreach (DictionaryEntry entry in dict)
            {
                var key = entry.Key?.ToString() ?? "";
                var value = entry.Value;

                if (value is IDictionary nestedDict && nestedDict.Count > 0)
                {
                    lines.Add($"{prefix}{key}:");
                    var nested = EncodeDictionary(nestedDict, indent, delimiter, level + 1);
                    if (!string.IsNullOrEmpty(nested))
                        lines.Add(nested);
                }
                else if (value is IEnumerable enumerable && !(value is string))
                {
                    var arr = enumerable.Cast<object?>().ToList();
                    var arrayStr = EncodeArray(arr, indent, delimiter, level);
                    if (arrayStr.StartsWith("["))
                    {
                        lines.Add($"{prefix}{key}{arrayStr}");
                    }
                    else
                    {
                        lines.Add($"{prefix}{key}{arrayStr}");
                    }
                }
                else
                {
                    var valueStr = EncodeValue(value, indent, delimiter, level);
                    lines.Add($"{prefix}{key}: {valueStr}");
                }
            }

            return string.Join("\n", lines);
        }

        private static string EncodeArray(List<object?> arr, int indent, string delimiter, int level)
        {
            if (arr.Count == 0)
                return "[0]:";

            if (IsUniformObjects(arr))
                return EncodeTabularArray(arr, indent, delimiter, level);

            if (IsPrimitiveArray(arr))
                return EncodePrimitiveArray(arr);

            return EncodeListArray(arr, indent, delimiter, level);
        }

        private static bool IsUniformObjects(List<object?> arr)
        {
            if (arr.Count == 0 || !(arr[0] is IDictionary firstDict))
                return false;

            var firstKeys = GetDictionaryKeys(firstDict);
            if (firstKeys.Count == 0)
                return false;

            foreach (var item in arr.Skip(1))
            {
                if (!(item is IDictionary dict))
                    return false;

                var keys = GetDictionaryKeys(dict);
                if (!KeysEqual(firstKeys, keys))
                    return false;

                foreach (var value in dict.Values)
                {
                    if (!IsPrimitive(value))
                        return false;
                }
            }

            return true;
        }

        private static List<string> GetDictionaryKeys(IDictionary dict)
        {
            return dict.Keys.Cast<object>().Select(k => k?.ToString() ?? "").ToList();
        }

        private static bool KeysEqual(List<string> keys1, List<string> keys2)
        {
            if (keys1.Count != keys2.Count)
                return false;

            return keys1.All(k => keys2.Contains(k)) && keys2.All(k => keys1.Contains(k));
        }

        private static bool IsPrimitive(object? value)
        {
            return value == null ||
                   value is string ||
                   value is int ||
                   value is long ||
                   value is short ||
                   value is byte ||
                   value is float ||
                   value is double ||
                   value is decimal ||
                   value is bool;
        }

        private static bool IsPrimitiveArray(List<object?> arr)
        {
            return arr.All(IsPrimitive);
        }

        private static string EncodeTabularArray(List<object?> arr, int indent, string delimiter, int level)
        {
            if (arr.Count == 0)
                return "[0]:";

            var firstDict = arr[0] as IDictionary ?? throw new InvalidOperationException();
            var keys = GetDictionaryKeys(firstDict);
            var n = arr.Count;

            var prefix = new string(' ', indent * level);
            var nextPrefix = new string(' ', indent * (level + 1));

            var keysStr = string.Join(delimiter, keys);
            var header = $"{prefix}[{n}]{{{keysStr}}}:";

            var lines = new List<string> { header };

            foreach (IDictionary item in arr)
            {
                var values = new List<string>();
                foreach (var key in keys)
                {
                    var value = item[key];
                    var valueStr = EncodeValue(value, indent, delimiter, level);
                    if (value is string str && !NeedsQuotesForTabular(str, delimiter))
                    {
                        values.Add(valueStr.Trim('"'));
                    }
                    else
                    {
                        values.Add(valueStr);
                    }
                }

                var row = string.Join(delimiter, values);
                lines.Add($"{nextPrefix}{row}");
            }

            return string.Join("\n", lines);
        }

        private static bool NeedsQuotesForTabular(string s, string delimiter)
        {
            return s.Contains(delimiter) ||
                   s.Contains(":") ||
                   s.Contains("\n") ||
                   s.Trim() != s ||
                   string.IsNullOrEmpty(s) ||
                   s.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                   s.Equals("false", StringComparison.OrdinalIgnoreCase) ||
                   s.Equals("null", StringComparison.OrdinalIgnoreCase);
        }

        private static string EncodePrimitiveArray(List<object?> arr)
        {
            var n = arr.Count;
            var values = arr.Select(item => EncodeValue(item, 2, ",", 0)).ToList();
            var valuesStr = string.Join(",", values);
            return $"[{n}]: {valuesStr}";
        }

        private static string EncodeListArray(List<object?> arr, int indent, string delimiter, int level)
        {
            var n = arr.Count;
            var prefix = new string(' ', indent * level);
            var nextPrefix = new string(' ', indent * (level + 1));

            var lines = new List<string> { $"{prefix}[{n}]:" };

            foreach (var item in arr)
            {
                if (item is IDictionary dict)
                {
                    var objStr = EncodeDictionary(dict, indent, delimiter, level + 1);
                    if (!string.IsNullOrEmpty(objStr))
                    {
                        var firstLine = objStr.Split('\n')[0];
                        lines.Add($"{nextPrefix}- {firstLine}");
                        if (objStr.Contains('\n'))
                        {
                            var rest = string.Join("\n", objStr.Split('\n').Skip(1));
                            lines.Add(rest);
                        }
                    }
                }
                else if (item is IEnumerable enumerable && !(item is string))
                {
                    var arrStr = EncodeArray(enumerable.Cast<object?>().ToList(), indent, delimiter, level + 1);
                    lines.Add($"{nextPrefix}- {arrStr}");
                }
                else
                {
                    var valueStr = EncodeValue(item, indent, delimiter, level);
                    lines.Add($"{nextPrefix}- {valueStr}");
                }
            }

            return string.Join("\n", lines);
        }
    }
}

