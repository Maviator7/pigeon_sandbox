using System;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

// Rewrites C# files with Roslyn's canonical whitespace. Only trivia changes, never tokens.
static class Formatter
{
    static int Main(string[] args)
    {
        bool check = args.Length > 0 && args[0] == "--check";
        var utf8 = new UTF8Encoding(false);
        int changed = 0;
        for (int i = check ? 1 : 0; i < args.Length; i++)
        {
            string path = args[i];
            string original = File.ReadAllText(path, utf8);
            string formatted = CSharpSyntaxTree.ParseText(original).GetRoot().NormalizeWhitespace("    ", "\n").ToFullString() + "\n";
            if (formatted == original)
                continue;
            changed++;
            if (check)
                Console.WriteLine("needs formatting: " + path);
            else
                File.WriteAllText(path, formatted, utf8);
        }

        return check && changed > 0 ? 1 : 0;
    }
}
