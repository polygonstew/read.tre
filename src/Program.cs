using System;
using System.IO;

namespace TreeWriter
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Write("path to scan (blank = current): ");
            string? input = Console.ReadLine(); // that ? gets me every time
            string rootPath = string.IsNullOrWhiteSpace(input) ? Directory.GetCurrentDirectory() : input;

            if (!Directory.Exists(rootPath))
            {
                Console.WriteLine("not a directory");
                return;
            }

            DirectoryInfo root = new DirectoryInfo(rootPath);
            string outFile = root.Name + ".tre";

            //write tree
            using (StreamWriter writer = new StreamWriter(outFile))
            {
                WriteTree(root, 0, writer);
            }

            Console.WriteLine("wrote " + outFile);
            Console.ReadKey();
        }

        //recursive
        static void WriteTree(DirectoryInfo dir, int depth, StreamWriter writer)
        {
            string indent = new string(' ', depth * 2);
            writer.WriteLine(indent + dir.Name + "/");

            foreach (DirectoryInfo sub in dir.GetDirectories())
            {
                if (sub.Name.StartsWith(".")) continue;
                WriteTree(sub, depth + 1, writer);
            }

            foreach (FileInfo file in dir.GetFiles())
            {
                writer.WriteLine(indent + "  " + file.Name);
            }
        }
    }
}