using System.Text;
using System.Text.RegularExpressions;

namespace read_tre;

class Program
{
    static void Main(string[] args)
    {
        bool includeHidden = false;
        string? target = null;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--hidden" || (args[i] == "-h" && i + 1 < args.Length && args[i + 1] == "-hidden"))
            {
                includeHidden = true;
            }
            else if (args[i] == "--help" || args[i] == "-h")
            {
                ShowHelp();
                return;
            }
            else if (!args[i].StartsWith('-'))
            {
                target = args[i];
            }
        }

        if (target == null && args.Length == 0)
        {
            Console.Write("path, .tre file, or '.' for current dir: ");
            target = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(target)) return;
        }

        if (File.Exists(target))
            MaterializeFromFile(target);
        else if (Directory.Exists(target))
            GenerateTreeFile(target, includeHidden);
        else
            Console.WriteLine($"Error: '{target}' does not exist as a file or directory.");
    }

    static void ShowHelp()
    {
        Console.WriteLine(@"
read.tre - bidirectional cli for .tre files

usage:
  tre                     scan current dir -> <name>.tre (prompts)
  tre <folder>            scan that folder -> <name>.tre
  tre <file.tre>          build folders from a .tre
  tre [path] --hidden     include hidden folders when scanning

flags:
  -h, --help              show this help
  --hidden                include folders starting with .

format:
  demo/
    readme.md
    src/
      main.py

also reads Windows 'tree /F /A' output
");
    }

    // Strips inline comments after #, //, or ( and trims trailing spaces
    static string StripComment(string line)
    {
        int commentIndex = -1;
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == '#')
            {
                commentIndex = i;
                break;
            }
            if (i + 1 < line.Length && line[i] == '/' && line[i + 1] == '/')
            {
                commentIndex = i;
                break;
            }
            if (line[i] == '(')
            {
                commentIndex = i;
                break;
            }
        }
        if (commentIndex >= 0)
            line = line.Substring(0, commentIndex);
        return line.TrimEnd();
    }

    static List<string> ExtractTreeLines(string[] allLines)
    {
        var result = new List<string>();
        bool inTree = false;

        for (int lineNum = 0; lineNum < allLines.Length; lineNum++)
        {
            string raw = allLines[lineNum];
            string line = StripComment(raw);

            if (string.IsNullOrWhiteSpace(line))
            {
                if (inTree) break;   // blank line ends tree block
                continue;
            }

            // Check for box-drawing characters (Windows tree /F or markdown trees)
            bool hasBoxDrawing = Regex.IsMatch(line, @"[├└│─\+\-\\|]");

            // Normal .tre lines start with spaces (indent) and end with '/' or contain a dot
            string trimmed = line.TrimStart();
            int leadingSpaces = line.Length - trimmed.Length;
            bool hasIndent = leadingSpaces > 0 || (lineNum == 0 && result.Count == 0);
            bool endsWithSlash = trimmed.EndsWith('/');
            bool hasFileExt = trimmed.Contains('.') && !trimmed.Contains(' ');

            bool looksLikeTree = hasBoxDrawing || (hasIndent && (endsWithSlash || hasFileExt));

            if (looksLikeTree)
            {
                inTree = true;
                result.Add(line);
            }
            else if (inTree)
            {
                break;
            }
        }
        return result;
    }

    static void MaterializeFromFile(string treFilePath)
    {
        if (!File.Exists(treFilePath))
        {
            Console.WriteLine($"File not found: {treFilePath}");
            return;
        }

        string[] rawLines = File.ReadAllLines(treFilePath, Encoding.UTF8);
        var treeLines = ExtractTreeLines(rawLines);
        if (treeLines.Count == 0)
        {
            Console.WriteLine("No valid tree lines found in the file.");
            return;
        }

        string rootName = Path.GetFileNameWithoutExtension(treFilePath);
        string basePath = Path.Combine(Directory.GetCurrentDirectory(), rootName);

        Console.WriteLine($"Materializing to: {basePath}");
        ParseAndMaterialize(treeLines.ToArray(), basePath);
    }

    static void ParseAndMaterialize(string[] lines, string basePath)
    {
        var stack = new Stack<(string Path, int Depth)>();
        stack.Push((basePath, -1));

        foreach (string rawLine in lines)
        {
            string line = rawLine.TrimEnd();
            if (string.IsNullOrEmpty(line)) continue;

            // Count depth differently for box-drawing vs space-indented
            int depth = 0;
            string entry = line;
            bool isFolder = entry.EndsWith('/');
            string name = isFolder ? entry.TrimEnd('/') : entry;

            // Try to detect box-drawing style (tree /F)
            bool hasBoxDrawing = Regex.IsMatch(line, @"[├└│─\+\-\\|]");
            if (hasBoxDrawing)
            {
                // Remove box-drawing prefixes to get the actual name
                name = Regex.Replace(line, @"^[├└│─\+\-\\| ]+", "");
                if (string.IsNullOrWhiteSpace(name))
                    continue;
                isFolder = name.EndsWith('/');
                if (isFolder) name = name.TrimEnd('/');
                // Estimate depth by counting how many "├", "└", "│", "+", "\", "|" appear before the name
                string prefix = line.Substring(0, line.IndexOf(name));
                depth = prefix.Count(c => c == '├' || c == '└' || c == '│' || c == '+' || c == '\\' || c == '|');
                // In tree /F, depth is number of vertical bars + 1? But we'll trust the stack logic.
                // For simplicity, we'll keep depth as counted, and stack will handle parents.
            }
            else
            {
                // Space-indented format: 2 spaces per level
                int leadingSpaces = line.TakeWhile(char.IsWhiteSpace).Count();
                depth = leadingSpaces / 2;
                entry = line.Trim();
                isFolder = entry.EndsWith('/');
                name = isFolder ? entry.TrimEnd('/') : entry;
            }

            // Adjust stack to the correct parent
            while (stack.Count > 1 && stack.Peek().Depth >= depth)
                stack.Pop();

            string parentPath = stack.Peek().Path;
            string fullPath = Path.Combine(parentPath, name);

            if (isFolder)
            {
                if (Directory.Exists(fullPath))
                    Console.WriteLine($"  folder exists: {fullPath} (merging)");
                else
                {
                    Directory.CreateDirectory(fullPath);
                    Console.WriteLine($"  created folder: {fullPath}");
                }
                stack.Push((fullPath, depth));
            }
            else
            {
                if (File.Exists(fullPath))
                    Console.WriteLine($"  file exists, skipping: {fullPath}");
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                    File.WriteAllText(fullPath, $"// auto-generated by read.tre\n// {name}\n");
                    Console.WriteLine($"  created file: {fullPath}");
                }
            }
        }
    }

    static void GenerateTreeFile(string directoryPath, bool includeHidden)
    {
        if (!Directory.Exists(directoryPath))
        {
            Console.WriteLine($"Directory not found: {directoryPath}");
            return;
        }

        var lines = new List<string>();
        WalkDirectory(directoryPath, "", lines, includeHidden);

        string outputFileName = Path.GetFileName(directoryPath) + ".tre";
        string outputPath = Path.Combine(Directory.GetCurrentDirectory(), outputFileName);
        File.WriteAllLines(outputPath, lines);
        Console.WriteLine($"wrote {outputPath}");
    }

    static void WalkDirectory(string dir, string indent, List<string> lines, bool includeHidden)
    {
        var entries = new List<(string Name, bool IsDir)>();

        foreach (string path in Directory.GetDirectories(dir))
        {
            string name = Path.GetFileName(path);
            if (!includeHidden && name.StartsWith('.')) continue;
            entries.Add((name, true));
        }
        foreach (string path in Directory.GetFiles(dir))
        {
            string name = Path.GetFileName(path);
            if (!includeHidden && name.StartsWith('.')) continue;
            entries.Add((name, false));
        }

        entries = entries.OrderBy(e => !e.IsDir).ThenBy(e => e.Name).ToList();

        for (int i = 0; i < entries.Count; i++)
        {
            var (name, isDir) = entries[i];
            bool isLast = i == entries.Count - 1;

            string prefix = indent + (isLast ? "  " : "  ");
            string entryLine = prefix + name;
            if (isDir) entryLine += "/";
            lines.Add(entryLine);

            if (isDir)
            {
                string newIndent = indent + (isLast ? "  " : "  ");
                WalkDirectory(Path.Combine(dir, name), newIndent, lines, includeHidden);
            }
        }
    }
}