using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace ReduceSvgFile
{
    class Program
    {
        static void Main(string[] args)
        {
            // Argument parsing variables
            string? selector = null;
            string? srcDir = null;
            string? outDir = null;
            int precision = 3;
            bool verbose = false;
            bool showHelp = false;

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                if (arg.Equals("-src", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length)
                    {
                        srcDir = args[++i];
                    }
                    else
                    {
                        Console.Error.WriteLine("Error: Missing value for -src argument.");
                        return;
                    }
                }
                else if (arg.Equals("-out", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length)
                    {
                        outDir = args[++i];
                    }
                    else
                    {
                        Console.Error.WriteLine("Error: Missing value for -out argument.");
                        return;
                    }
                }
                else if (arg.Equals("-p", StringComparison.OrdinalIgnoreCase) || arg.Equals("--precision", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length && int.TryParse(args[i + 1], out int p))
                    {
                        precision = p;
                        i++;
                    }
                    else
                    {
                        Console.Error.WriteLine("Error: Missing or invalid value for precision.");
                        return;
                    }
                }
                else if (arg.Equals("-v", StringComparison.OrdinalIgnoreCase) || arg.Equals("--verbose", StringComparison.OrdinalIgnoreCase))
                {
                    verbose = true;
                }
                else if (arg.Equals("-h", StringComparison.OrdinalIgnoreCase) || arg.Equals("--help", StringComparison.OrdinalIgnoreCase))
                {
                    showHelp = true;
                }
                else
                {
                    // Positional arguments
                    if (selector == null)
                    {
                        selector = arg;
                    }
                    else
                    {
                        Console.Error.WriteLine($"Error: Unexpected argument '{arg}'.");
                        return;
                    }
                }
            }

            if (showHelp || selector == null)
            {
                PrintHelp();
                return;
            }

            // Resolve directories
            string currentDir = Directory.GetCurrentDirectory();
            string resolvedSrcDir = srcDir != null ? Path.GetFullPath(Path.Combine(currentDir, srcDir)) : currentDir;
            bool hasOutDir = outDir != null;
            string? resolvedOutDir = hasOutDir ? Path.GetFullPath(Path.Combine(currentDir, outDir!)) : null;

            // Find target files
            List<string> targetFiles = new List<string>();

            if (selector == "*")
            {
                if (!Directory.Exists(resolvedSrcDir))
                {
                    Console.Error.WriteLine($"Error: Source directory '{resolvedSrcDir}' does not exist.");
                    return;
                }
                targetFiles.AddRange(Directory.GetFiles(resolvedSrcDir, "*.svg"));
                if (targetFiles.Count == 0)
                {
                    Console.WriteLine("No .svg files found in the source directory.");
                    return;
                }
            }
            else
            {
                // Single file selector
                string fullPath = Path.IsPathRooted(selector) ? selector : Path.Combine(resolvedSrcDir, selector);
                fullPath = Path.GetFullPath(fullPath);

                if (!File.Exists(fullPath))
                {
                    Console.Error.WriteLine($"Error: File '{fullPath}' does not exist.");
                    return;
                }
                targetFiles.Add(fullPath);
            }

            // Create output directory if specified
            if (hasOutDir && resolvedOutDir != null && !Directory.Exists(resolvedOutDir))
            {
                try
                {
                    Directory.CreateDirectory(resolvedOutDir);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error: Failed to create output directory '{resolvedOutDir}'. Details: {ex.Message}");
                    return;
                }
            }

            // Process each file
            int processedCount = 0;
            int failedCount = 0;
            long totalOriginalBytes = 0;
            long totalOptimizedBytes = 0;
            string? lastOutputFile = null;

            foreach (var inputFile in targetFiles)
            {
                string inputFileName = Path.GetFileName(inputFile);
                string inputDir = Path.GetDirectoryName(inputFile) ?? resolvedSrcDir;

                string outputFile;
                if (hasOutDir && resolvedOutDir != null)
                {
                    outputFile = Path.Combine(resolvedOutDir, inputFileName);
                }
                else
                {
                    string baseName = Path.GetFileNameWithoutExtension(inputFile);
                    string ext = Path.GetExtension(inputFile);
                    outputFile = Path.Combine(inputDir, $"{baseName}_out{ext}");
                }

                lastOutputFile = outputFile;

                try
                {
                    string originalContent = File.ReadAllText(inputFile);
                    long originalBytes = new FileInfo(inputFile).Length;

                    string optimizedContent = SvgOptimizer.Optimize(originalContent, precision);
                    
                    File.WriteAllText(outputFile, optimizedContent, new System.Text.UTF8Encoding(false));
                    long optimizedBytes = new FileInfo(outputFile).Length;

                    totalOriginalBytes += originalBytes;
                    totalOptimizedBytes += optimizedBytes;
                    processedCount++;

                    if (verbose)
                    {
                        double reduction = originalBytes > 0 ? (1.0 - (double)optimizedBytes / originalBytes) * 100.0 : 0.0;
                        Console.WriteLine($"Optimized: {inputFileName}");
                        Console.WriteLine($"  Input Path:  {inputFile}");
                        Console.WriteLine($"  Output Path: {outputFile}");
                        Console.WriteLine($"  Size:        {originalBytes:N0} bytes -> {optimizedBytes:N0} bytes ({reduction:F2}% reduction)");
                        Console.WriteLine();
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error: Failed to process file '{inputFile}'. Details: {ex.Message}");
                    failedCount++;
                }
            }

            // Final summary
            if (verbose || processedCount > 1)
            {
                Console.WriteLine("---------------------------------------------");
                Console.WriteLine($"Finished processing. Success: {processedCount}, Failed: {failedCount}.");
                if (processedCount > 0)
                {
                    double totalReduction = totalOriginalBytes > 0 ? (1.0 - (double)totalOptimizedBytes / totalOriginalBytes) * 100.0 : 0.0;
                    Console.WriteLine($"Total size: {totalOriginalBytes:N0} bytes -> {totalOptimizedBytes:N0} bytes ({totalReduction:F2}% reduction)");
                }
                Console.WriteLine("---------------------------------------------");
            }
            else if (processedCount == 1 && !verbose && lastOutputFile != null)
            {
                Console.WriteLine($"Successfully optimized SVG. Output saved to: {lastOutputFile}");
            }
        }

        static void PrintHelp()
        {
            Console.WriteLine("reducesvg - A command-line tool to optimize SVG files.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  reducesvg <file.svg> [options]");
            Console.WriteLine("  reducesvg * -src <directory> [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -src <path>            Specify the source directory (defaults to current directory).");
            Console.WriteLine("  -out <path>            Specify the output directory. Output files will retain their original");
            Console.WriteLine("                         names (no '_out' suffix) and be saved to this folder.");
            Console.WriteLine("  -p, --precision <num>  Set decimal coordinate precision (default: 3).");
            Console.WriteLine("  -v, --verbose          Print conversion and compression statistics.");
            Console.WriteLine("  -h, --help             Show this help message.");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  reducesvg file.svg");
            Console.WriteLine("    Optimizes 'file.svg' and saves it as 'file_out.svg' in the same directory.");
            Console.WriteLine();
            Console.WriteLine("  reducesvg * -src \"C:\\Projects\\Assets\"");
            Console.WriteLine("    Optimizes all SVGs in the Assets directory and saves them with '_out' in the same folder.");
            Console.WriteLine();
            Console.WriteLine("  reducesvg * -src \"C:\\Src\" -out \"C:\\Out\" -p 2 -v");
            Console.WriteLine("    Optimizes all SVGs in 'C:\\Src', outputs them to 'C:\\Out' without renaming, rounds to 2 decimals,");
            Console.WriteLine("    and prints detailed statistics.");
        }
    }
}
