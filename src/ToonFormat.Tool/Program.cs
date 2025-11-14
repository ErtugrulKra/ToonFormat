using System;
using System.IO;
using System.Text.Json;

namespace ToonFormat.Tool
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintUsage();
                return 1;
            }

            try
            {
                var command = args[0].ToLower();

                switch (command)
                {
                    case "encode":
                        return HandleEncode(args);
                    case "decode":
                        return HandleDecode(args);
                    case "--version":
                    case "-v":
                        PrintVersion();
                        return 0;
                    case "--help":
                    case "-h":
                        PrintHelp();
                        return 0;
                    default:
                        Console.Error.WriteLine($"Unknown command: {command}");
                        PrintUsage();
                        return 1;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        static int HandleEncode(string[] args)
        {
            string? inputFile = null;
            string? outputFile = null;
            int indent = 2;
            string delimiter = ",";

            for (int i = 1; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-o":
                    case "--output":
                        if (i + 1 < args.Length)
                        {
                            outputFile = args[++i];
                        }
                        else
                        {
                            Console.Error.WriteLine("Error: --output requires a file path");
                            return 1;
                        }
                        break;
                    case "-i":
                    case "--indent":
                        if (i + 1 < args.Length && int.TryParse(args[++i], out var indentValue))
                        {
                            indent = indentValue;
                        }
                        else
                        {
                            Console.Error.WriteLine("Error: --indent requires a number");
                            return 1;
                        }
                        break;
                    case "-d":
                    case "--delimiter":
                        if (i + 1 < args.Length)
                        {
                            delimiter = args[++i];
                        }
                        else
                        {
                            Console.Error.WriteLine("Error: --delimiter requires a character");
                            return 1;
                        }
                        break;
                    default:
                        if (inputFile == null && !args[i].StartsWith("-"))
                        {
                            inputFile = args[i];
                        }
                        break;
                }
            }

            object? data;

            // Read input
            if (inputFile != null)
            {
                if (!File.Exists(inputFile))
                {
                    Console.Error.WriteLine($"Error: File not found: {inputFile}");
                    return 1;
                }
                var jsonContent = File.ReadAllText(inputFile);
                data = JsonSerializer.Deserialize<object>(jsonContent);
            }
            else
            {
                // Read from stdin
                var jsonContent = Console.In.ReadToEnd();
                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    Console.Error.WriteLine("Error: No input provided");
                    return 1;
                }
                data = JsonSerializer.Deserialize<object>(jsonContent);
            }

            // Convert to TOON
            var toonOutput = ToonFormat.Encode(data, indent, delimiter);

            // Write output
            if (outputFile != null)
            {
                var directory = Path.GetDirectoryName(outputFile);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                File.WriteAllText(outputFile, toonOutput);
            }
            else
            {
                Console.Write(toonOutput);
            }

            return 0;
        }

        static int HandleDecode(string[] args)
        {
            string? inputFile = null;
            string? outputFile = null;
            int indent = 2;
            bool strict = true;

            for (int i = 1; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-o":
                    case "--output":
                        if (i + 1 < args.Length)
                        {
                            outputFile = args[++i];
                        }
                        else
                        {
                            Console.Error.WriteLine("Error: --output requires a file path");
                            return 1;
                        }
                        break;
                    case "-i":
                    case "--indent":
                        if (i + 1 < args.Length && int.TryParse(args[++i], out var indentValue))
                        {
                            indent = indentValue;
                        }
                        else
                        {
                            Console.Error.WriteLine("Error: --indent requires a number");
                            return 1;
                        }
                        break;
                    case "--no-strict":
                        strict = false;
                        break;
                    default:
                        if (inputFile == null && !args[i].StartsWith("-"))
                        {
                            inputFile = args[i];
                        }
                        break;
                }
            }

            string toonInput;

            // Read input
            if (inputFile != null)
            {
                if (!File.Exists(inputFile))
                {
                    Console.Error.WriteLine($"Error: File not found: {inputFile}");
                    return 1;
                }
                toonInput = File.ReadAllText(inputFile);
            }
            else
            {
                // Read from stdin
                toonInput = Console.In.ReadToEnd();
                if (string.IsNullOrWhiteSpace(toonInput))
                {
                    Console.Error.WriteLine("Error: No input provided");
                    return 1;
                }
            }

            // Convert to JSON
            var data = ToonFormat.Decode(toonInput, indent, strict);
            var jsonOutput = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            // Write output
            if (outputFile != null)
            {
                var directory = Path.GetDirectoryName(outputFile);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                File.WriteAllText(outputFile, jsonOutput);
            }
            else
            {
                Console.Write(jsonOutput);
            }

            return 0;
        }

        static void PrintUsage()
        {
            Console.WriteLine("ToonFormat Tool - TOON format converter");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  toon <command> [options]");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  encode    Convert JSON to TOON format");
            Console.WriteLine("  decode    Convert TOON to JSON format");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -h, --help       Show help");
            Console.WriteLine("  -v, --version    Show version");
            Console.WriteLine();
            Console.WriteLine("Use 'toon <command> --help' for command-specific help");
        }

        static void PrintHelp()
        {
            PrintUsage();
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  # Convert JSON to TOON");
            Console.WriteLine("  toon encode input.json");
            Console.WriteLine("  toon encode input.json -o output.toon");
            Console.WriteLine();
            Console.WriteLine("  # Convert TOON to JSON");
            Console.WriteLine("  toon decode input.toon");
            Console.WriteLine("  toon decode input.toon -o output.json");
            Console.WriteLine();
            Console.WriteLine("  # Read from stdin");
            Console.WriteLine("  echo '{\"key\": \"value\"}' | toon encode");
            Console.WriteLine("  cat data.toon | toon decode");
        }

        static void PrintVersion()
        {
            Console.WriteLine("ToonFormat Tool version 0.1.0");
        }
    }
}

