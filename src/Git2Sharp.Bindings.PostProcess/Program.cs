using System.Text;
using Microsoft.CodeAnalysis.CSharp;

namespace Git2Sharp.Bindings.PostProcess;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length != 1 || !Directory.Exists(args[0]))
        {
            Console.WriteLine("usage: dotnet run --project src/Git2Sharp.Bindings.PostProcess -- <directory>");
            return 2;
        }

        CSharpParseOptions parseOptions = new CSharpParseOptions(LanguageVersion.CSharp10);
        List<SourceFileInfo> files = Directory.EnumerateFiles(args[0], "*.cs", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(path =>
            {
                string text = File.ReadAllText(path);
                return new SourceFileInfo
                {
                    Path = path,
                    Text = text,
                    Tree = CSharpSyntaxTree.ParseText(text)
                };
            })
            .ToList();

        if (files.Count == 0)
        {
            Console.Error.WriteLine($"error: no *.cs files in '{args[0]}'");
            return 1;
        }

        Collector collector = new Collector();
        foreach (SourceFileInfo file in files)
        {
            collector.Visit(file.Tree.GetRoot());
        }

        Dictionary<string, string> renames = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string name in collector.SnakeCaseTypes.Concat(collector.ImportedFunctions))
        {
            renames[name] = Naming.ToPascalCase(name);
        }

        List<string> problems = new List<string>();
        foreach (IGrouping<string, KeyValuePair<string, string>> group in renames.GroupBy(pair => pair.Value).Where(group => group.Count() > 1))
        {
            problems.Add($"{string.Join(", ", group.Select(pair => pair.Key))} all map to '{group.Key}'");
        }

        foreach (KeyValuePair<string, string> pair in renames.Where(pair => collector.AllDeclaredNames.Contains(pair.Value)))
        {
            problems.Add($"'{pair.Key}' maps to '{pair.Value}', which is already declared");
        }

        if (problems.Any())
        {
            foreach (string problem in problems)
            {
                Console.Error.WriteLine(problem);
            }

            return 1;
        }

        Rewriter rewriter = new Rewriter(renames, collector.ImportedFunctions, collector.EnumMembers);
        int changedFiles = 0;
        foreach (SourceFileInfo file in files)
        {
            string newText = rewriter.Visit(file.Tree.GetRoot()).ToFullString();
            if (newText != file.Text)
            {
                File.WriteAllText(file.Path, newText, new UTF8Encoding(false));
                changedFiles++;
            }
        }

        Console.WriteLine(
            $"PostProcess: renamed {collector.SnakeCaseTypes.Count} types, {collector.ImportedFunctions.Count} functions, and {collector.SnakeCaseProperties.Count} properties, " +
            $"cast {rewriter.ConstCasts} enum references in constants, rewrote {changedFiles}/{files.Count} files"
        );
        return 0;
    }
}