using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Git2Sharp.Bindings.PostProcess;

public static class Naming
{
    public static string ToPascalCase(string snakeCase)
        => string.Concat(
            snakeCase.Split("_", StringSplitOptions.RemoveEmptyEntries)
                .Select(part => char.ToUpperInvariant(part[0]) + part.Substring(1))
        );

    public static readonly Regex SnakeCase = new Regex("^[a-z][a-z0-9]*(_[a-z0-9_]+)*$", RegexOptions.Compiled);

    public static bool IsImport(MethodDeclarationSyntax method)
        => method.AttributeLists.SelectMany(list => list.Attributes)
            .Any(IsImportAttribute);

    public static bool IsImportAttribute(AttributeSyntax attribute)
    {
        string name = attribute.Name.ToString().Split('.').Last();
        return (
            name == "DllImport" ||
            name == "DllImportAttribute" ||
            name == "LibraryImport" ||
            name == "LibraryImportAttribute"
        );
    }
}