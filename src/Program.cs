using System;
using System.IO;
using System.Collections.Generic;

namespace TreeWriter
{
    class Program
    {
        static void Main(string[] args)
        {
            string target = "";
            bool includeHidden = false;
            bool showHelp = false;
            foreach (string a in args)
            {
                if (a == "--hidden") includeHidden = true;
                else if (a == "-h" || a == "--help") showHelp = true;
                else target = a;
            }

            if (showHelp)
            {
                PrintHelp();
                return;
            }

            //no target -> prompt
            if (string.IsNullOrEmpty(target))
            {
                Console.Write("path, .tre file, or '.' for current dir: ");
                target = Console.ReadLine()?.Trim() ?? "";
                if (string.IsNullOrEmpty(target)) return;
                if (target == ".") target = Directory.GetCurrentDirectory();
            }

            if (File.Exists(target))
            {
                BuildFromTree(target);
            }
            else if (Directory.Exists(target))
            {
                ScanDirectory(target, includeHidden);
            }
            else
            {
                Console.WriteLine("not found: " + target);
            }
        }

        static void PrintHelp()
        {
            Console.WriteLine(@"read.tre - bidirectional cli for .tre files

usage:
  tre                    scan current dir -> <name>.tre
  tre <folder>           scan that folder -> <name>.tre
  tre <file.tre>         build folders from a .tre
  tre [path] --hidden    include hidden folders when scanning

flags:
  -h, --help             show this help
  --hidden               include folders starting with .

format:
  demo/
    readme.md
    src/
      main.py

also reads windows 'tree /F /A' output");
        }

        //forward: dir -> .tre
        static void ScanDirectory(string rootPath, bool includeHidden)
        {
            DirectoryInfo root = new DirectoryInfo(rootPath);
            string outFile = root.Name + ".tre";

            using (StreamWriter writer = new StreamWriter(outFile))
            {
                WriteTree(root, 0, writer, includeHidden);
            }

            Console.WriteLine("wrote " + outFile);
        }

        //recursive
        static void WriteTree(DirectoryInfo dir, int depth, StreamWriter writer, bool includeHidden)
        {
            string indent = new string(' ', depth * 2);
            writer.WriteLine(indent + dir.Name + "/");

            try
            {
                foreach (DirectoryInfo sub in dir.GetDirectories())
                {
                    if (!includeHidden && sub.Name.StartsWith(".")) continue;
                    WriteTree(sub, depth + 1, writer, includeHidden);
                }

                foreach (FileInfo file in dir.GetFiles())
                {
                    writer.WriteLine(indent + "  " + file.Name);
                }
            }
            catch (UnauthorizedAccessException) { }
        }

        //reverse: .tre -> dir
        static void BuildFromTree(string treFile)
        {
            string[] lines = File.ReadAllLines(treFile);

            //detect tree /F format
            bool treeFormat = false;
            foreach (string l in lines)
            {
                if (l.Contains("PATH listing") || l.Contains("├") || l.Contains("└")
                    || l.Contains("+---") || l.Contains("\\---"))
                {
                    treeFormat = true;
                    break;
                }
            }

            if (treeFormat)
            {
                string rootName = Path.GetFileNameWithoutExtension(treFile);
                lines = ConvertTreeFormat(lines, rootName);
            }

            Materialize(lines);
            Console.WriteLine("built from " + treFile);
        }

        //tree /F output -> our simple format
        static string[] ConvertTreeFormat(string[] lines, string rootName)
        {
            List<string> result = new List<string>();
            result.Add(rootName + "/");

            foreach (string raw in lines)
            {
                string line = raw.TrimEnd();
                if (string.IsNullOrEmpty(line)) continue;
                if (line.Contains("PATH listing")) continue;
                if (line.StartsWith("Volume serial number")) continue;
                if (line.EndsWith(":.")) continue;

                //skip past pipes/brackets/dashes/spaces to find the name
                int nameStart = 0;
                while (nameStart < line.Length)
                {
                    char c = line[nameStart];
                    bool isPrefix = c == ' ' || c == '|' || c == '+' || c == '\\' || c == '-'
                        || c == '│' || c == '├' || c == '└' || c == '─';
                    if (!isPrefix) break;
                    nameStart++;
                }

                if (nameStart >= line.Length) continue;

                int depth = nameStart / 4;
                if (depth < 1) continue;

                string name = line.Substring(nameStart);

                //folder if there's a tree marker right before the name
                bool isFolder = false;
                int markerPos = (depth - 1) * 4;
                if (markerPos >= 0 && markerPos < line.Length)
                {
                    char m = line[markerPos];
                    if (m == '├' || m == '└' || m == '+' || m == '\\') isFolder = true;
                }

                string indent = new string(' ', depth * 2);
                result.Add(indent + name + (isFolder ? "/" : ""));
            }

            return result.ToArray();
        }

        //simple format -> create folders/files
        static void Materialize(string[] lines)
        {
            List<string> stack = new List<string>();

            foreach (string raw in lines)
            {
                string line = raw.TrimEnd();
                if (string.IsNullOrEmpty(line)) continue;

                int spaces = 0;
                while (spaces < line.Length && line[spaces] == ' ') spaces++;
                int depth = spaces / 2;

                string name = line.Substring(spaces);
                bool isFolder = name.EndsWith("/");
                if (isFolder) name = name.Substring(0, name.Length - 1);

                //pop back to parent depth
                while (stack.Count > depth) stack.RemoveAt(stack.Count - 1);

                stack.Add(name);
                string path = Path.Combine(stack.ToArray());

                if (isFolder)
                {
                    if (Directory.Exists(path)) Console.WriteLine("exists:  " + path + "/");
                    Directory.CreateDirectory(path);
                }
                else
                {
                    string parent = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
                    if (File.Exists(path))
                    {
                        Console.WriteLine("skipped: " + path);
                    }
                    else
                    {
                        File.Create(path).Dispose();
                    }
                    stack.RemoveAt(stack.Count - 1);
                }
            }
        }
    }
}