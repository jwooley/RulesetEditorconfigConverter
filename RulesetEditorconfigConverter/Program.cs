using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace RulesetEditorconfigConverter
{
    class Program
    {
        static void Main(string[] args)
        {
            var rootPath = ".";

            if (args is object && args.Length > 0)
            {
                rootPath = args[0];
            }
            RecursePath(new DirectoryInfo(rootPath));
        }

        static void RecursePath(DirectoryInfo directory)
        {
            Dictionary<string, string> existingRules = null;
            foreach (var sourceFile in directory.GetFiles("*.ruleset"))
            {
                if (sourceFile.FullName.Contains("packages"))
                {
                    continue;
                }
                Console.WriteLine($"Converting {sourceFile.FullName}");
                existingRules ??= ParseExistingEditorConfig(directory.FullName);
                ParseRuleset(sourceFile, existingRules);
            }
            foreach (var child in directory.EnumerateDirectories())
            {
                RecursePath(child);
            }
        }

        private static void ParseRuleset(FileInfo sourceFile, Dictionary<string, string> existingRules)
        {
            var xFile = XElement.Load(sourceFile.FullName);
            var rules = from rule in xFile.Descendants("Rule")
                        select new
                        {
                            Id = rule.Attribute("Id").Value,
                            Severity = rule.Attribute("Action").Value
                        };

            using var editorconfig = new StreamWriter(Path.Combine(sourceFile.DirectoryName, ".editorconfig"), append: true);
            foreach (var rule in rules)
            {
                if (!existingRules.ContainsKey(rule.Id))
                {
                    editorconfig.WriteLine($"dotnet_diagnostic.{rule.Id}.severity = {rule.Severity.ToLower()}");
                    existingRules.Add(rule.Id, rule.Severity);
                }
            }
        }

        private static void CreateEditorConfig(string directory)
        {
            using var writer = new StreamWriter(Path.Combine(directory, ".editorconfig"));
            writer.WriteLine("[*.{cs,vb}]");
        }

        static Dictionary<string, string> ParseExistingEditorConfig(string path)
        {
            var items = new Dictionary<string, string>();
            try
            {
                var existingConfig = Path.Combine(path, ".editorconfig");
                if (File.Exists(existingConfig))
                {
                    using var reader = new StreamReader(existingConfig);
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (!string.IsNullOrEmpty(line) && line.StartsWith("dotnet_diagnostic"))
                        {
                            var keyValue = line.Split('=', StringSplitOptions.RemoveEmptyEntries);
                            var keyNumb = keyValue[0].Split('.');
                            if (!items.ContainsKey(keyNumb[1]))
                            {
                                items.Add(keyNumb[1], keyValue[1].Trim());
                            }
                        }
                    }
                }
                else
                {
                    CreateEditorConfig(path);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return items;
        }
    }
}
