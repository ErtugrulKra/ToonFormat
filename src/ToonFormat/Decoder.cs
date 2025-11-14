using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ToonFormat
{
    internal static class Decoder
    {
        public static object? Decode(string toonString, int indent, bool strict)
        {
            var lines = toonString.Split('\n')
                .Select(line => line.TrimEnd())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();

            if (lines.Count == 0)
                return new Dictionary<string, object?>();

            var parser = new ToonParser(lines, indent, strict);
            return parser.Parse();
        }
    }

    internal class ToonParser
    {
        private readonly List<string> _lines;
        private readonly int _indent;
        private readonly bool _strict;
        private int _pos;

        public ToonParser(List<string> lines, int indent, bool strict)
        {
            _lines = lines;
            _indent = indent;
            _strict = strict;
            _pos = 0;
        }

        public object? Parse()
        {
            if (_lines.Count == 0)
                return new Dictionary<string, object?>();

            var firstLine = _lines[0].TrimStart();
            if (firstLine.StartsWith("["))
            {
                return ParseArray(firstLine, 0);
            }

            return ParseObject(0);
        }

        private Dictionary<string, object?> ParseObject(int level)
        {
            var obj = new Dictionary<string, object?>();
            var prefix = new string(' ', _indent * level);

            while (_pos < _lines.Count)
            {
                var line = _lines[_pos];

                if (!line.StartsWith(prefix) && line != line.TrimStart())
                    break;

                var stripped = line.TrimStart();
                if (string.IsNullOrEmpty(stripped))
                {
                    _pos++;
                    continue;
                }

                if (line.StartsWith(prefix) && line.Length - stripped.Length < prefix.Length)
                    break;

                if (stripped.Contains(":"))
                {
                    var arrayMatch = Regex.Match(stripped, @"^([^:\[\]]+)(\[(\d+)\](?:\{([^}]+)\})?):\s*(.*)$");
                    if (arrayMatch.Success)
                    {
                        var arrayKey = arrayMatch.Groups[1].Value.Trim();
                        var n = int.Parse(arrayMatch.Groups[3].Value);
                        var fieldsStr = arrayMatch.Groups[4].Value;
                        var arrayValuePart = arrayMatch.Groups[5].Value;

                        _pos++;

                        if (!string.IsNullOrEmpty(fieldsStr))
                        {
                            var delimiter = DetectDelimiter(fieldsStr);
                            var fields = fieldsStr.Split(new[] { delimiter }, StringSplitOptions.None).Select(f => f.Trim()).ToList();
                            var arr = ParseTabularArray(n, fields, delimiter, level);
                            obj[arrayKey] = arr;
                        }
                        else if (!string.IsNullOrEmpty(arrayValuePart))
                        {
                            var arr = ParsePrimitiveArray(arrayValuePart);
                            obj[arrayKey] = arr;
                        }
                        else
                        {
                            var arr = ParseListArray(n, level);
                            obj[arrayKey] = arr;
                        }
                        continue;
                    }

                    var colonIndex = stripped.IndexOf(':');
                    var key = stripped.Substring(0, colonIndex).Trim();
                    var valuePart = stripped.Substring(colonIndex + 1).Trim();

                    if (valuePart.StartsWith("["))
                    {
                        var arr = ParseArray(valuePart, level);
                        obj[key] = arr;
                        _pos++;
                    }
                    else if (string.IsNullOrEmpty(valuePart))
                    {
                        if (_pos + 1 < _lines.Count)
                        {
                            var nextLine = _lines[_pos + 1];
                            var nextPrefix = new string(' ', _indent * (level + 1));
                            if (nextLine.StartsWith(nextPrefix))
                            {
                                var nextStripped = nextLine.TrimStart();
                                if (nextStripped.StartsWith("["))
                                {
                                    _pos++;
                                    var arr = ParseArray(nextStripped, level);
                                    obj[key] = arr;
                                    continue;
                                }
                                else
                                {
                                    _pos++;
                                    var nested = ParseObject(level + 1);
                                    obj[key] = nested;
                                    continue;
                                }
                            }
                        }
                        obj[key] = null;
                        _pos++;
                    }
                    else
                    {
                        obj[key] = ParsePrimitive(valuePart);
                        _pos++;
                    }
                }
                else
                {
                    _pos++;
                }
            }

            return obj;
        }

        private object? ParseArray(string header, int level)
        {
            var colonIndex = header.IndexOf(':');
            var valuePart = colonIndex != -1 ? header.Substring(colonIndex + 1).Trim() : "";
            var headerPart = colonIndex != -1 ? header.Substring(0, colonIndex + 1) : header;

            var match = Regex.Match(headerPart, @"\[(\d+)\](?:\{([^}]+)\})?:");
            if (!match.Success)
            {
                if (_strict)
                    throw new FormatException($"Invalid array header: {headerPart}");
                return new List<object?>();
            }

            var n = int.Parse(match.Groups[1].Value);
            var fieldsStr = match.Groups[2].Value;

            if (!string.IsNullOrEmpty(fieldsStr))
            {
                var delimiter = DetectDelimiter(fieldsStr);
                var fields = fieldsStr.Split(new[] { delimiter }, StringSplitOptions.None).Select(f => f.Trim()).ToList();
                return ParseTabularArray(n, fields, delimiter, level);
            }
            else if (!string.IsNullOrEmpty(valuePart))
            {
                return ParsePrimitiveArray(valuePart);
            }
            else
            {
                return ParseListArray(n, level);
            }
        }

        private string DetectDelimiter(string fieldsStr)
        {
            if (fieldsStr.Contains('\t'))
                return "\t";
            if (fieldsStr.Contains('|'))
                return "|";
            return ",";
        }

        private List<Dictionary<string, object?>> ParseTabularArray(int n, List<string> fields, string delimiter, int level)
        {
            var arr = new List<Dictionary<string, object?>>();
            var prefix = new string(' ', _indent * (level + 1));

            for (int i = 0; i < n; i++)
            {
                if (_pos >= _lines.Count)
                {
                    if (_strict)
                        throw new FormatException($"Expected {n} rows, got {i}");
                    break;
                }

                var line = _lines[_pos];
                if (!line.StartsWith(prefix))
                {
                    if (_strict)
                        throw new FormatException($"Unexpected indentation at row {i + 1}");
                    break;
                }

                var stripped = line.TrimStart();
                var values = SplitRow(stripped, delimiter);

                if (values.Count != fields.Count)
                {
                    if (_strict)
                        throw new FormatException($"Row {i + 1}: expected {fields.Count} values, got {values.Count}");
                }

                var item = new Dictionary<string, object?>();
                for (int j = 0; j < fields.Count; j++)
                {
                    if (j < values.Count)
                    {
                        item[fields[j]] = ParsePrimitive(values[j].Trim());
                    }
                    else
                    {
                        item[fields[j]] = null;
                    }
                }

                arr.Add(item);
                _pos++;
            }

            return arr;
        }

        private List<string> SplitRow(string row, string delimiter)
        {
            var parts = new List<string>();
            var current = "";
            var inQuotes = false;
            var escapeNext = false;

            foreach (var ch in row)
            {
                if (escapeNext)
                {
                    current += ch;
                    escapeNext = false;
                }
                else if (ch == '\\')
                {
                    escapeNext = true;
                    current += ch;
                }
                else if (ch == '"')
                {
                    inQuotes = !inQuotes;
                    current += ch;
                }
                else if (ch.ToString() == delimiter && !inQuotes)
                {
                    parts.Add(current);
                    current = "";
                }
                else
                {
                    current += ch;
                }
            }

            if (!string.IsNullOrEmpty(current))
                parts.Add(current);

            return parts;
        }

        private List<object?> ParsePrimitiveArray(string valueStr)
        {
            var values = valueStr.Split(',').Select(v => v.Trim()).ToList();
            return values.Select(ParsePrimitive).ToList();
        }

        private List<object?> ParseListArray(int n, int level)
        {
            var arr = new List<object?>();
            var prefix = new string(' ', _indent * (level + 1));

            for (int i = 0; i < n; i++)
            {
                if (_pos >= _lines.Count)
                {
                    if (_strict)
                        throw new FormatException($"Expected {n} items, got {i}");
                    break;
                }

                var line = _lines[_pos];
                if (!line.StartsWith(prefix))
                {
                    if (_strict)
                        throw new FormatException($"Unexpected indentation at item {i + 1}");
                    break;
                }

                var stripped = line.TrimStart();
                if (!stripped.StartsWith("- "))
                {
                    if (_strict)
                        throw new FormatException($"Expected list item marker '- ' at item {i + 1}");
                    break;
                }

                var itemStr = stripped.Substring(2).Trim();

                if (itemStr.StartsWith("["))
                {
                    var item = ParseArray(itemStr, level + 1);
                    arr.Add(item);
                }
                else if (itemStr.Contains(":") && !itemStr.StartsWith("\""))
                {
                    var tempLines = new List<string> { itemStr };
                    var oldPos = _pos;
                    _pos = 0;
                    var tempParser = new ToonParser(tempLines, _indent, _strict);
                    var item = tempParser.ParseObject(0);
                    _pos = oldPos;
                    arr.Add(item);
                }
                else
                {
                    var item = ParsePrimitive(itemStr);
                    arr.Add(item);
                }

                _pos++;
            }

            return arr;
        }

        private object? ParsePrimitive(string valueStr)
        {
            valueStr = valueStr.Trim();

            if (string.IsNullOrEmpty(valueStr))
                return null;

            if (valueStr.StartsWith("\"") && valueStr.EndsWith("\""))
            {
                var inner = valueStr.Substring(1, valueStr.Length - 2);
                return inner.Replace("\\\"", "\"").Replace("\\\\", "\\");
            }

            if (valueStr.Equals("true", StringComparison.OrdinalIgnoreCase))
                return true;

            if (valueStr.Equals("false", StringComparison.OrdinalIgnoreCase))
                return false;

            if (valueStr.Equals("null", StringComparison.OrdinalIgnoreCase))
                return null;

            if (int.TryParse(valueStr, out var intValue))
                return intValue;

            if (double.TryParse(valueStr, out var doubleValue))
                return doubleValue;

            return valueStr;
        }
    }
}

