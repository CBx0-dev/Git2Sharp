using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Git2Sharp.Bindings.PostProcess;

public sealed class Collector : CSharpSyntaxWalker
{
    public HashSet<string> SnakeCaseTypes { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> ImportedFunctions { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> SnakeCaseProperties { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> EnumMembers { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> AllDeclaredNames { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public override void VisitStructDeclaration(StructDeclarationSyntax node)
    {
        AddType(node.Identifier);
        base.VisitStructDeclaration(node);
    }

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        AddType(node.Identifier);
        base.VisitClassDeclaration(node);
    }

    public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
    {
        AddType(node.Identifier);
        base.VisitInterfaceDeclaration(node);
    }

    public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
    {
        AddType(node.Identifier);
        base.VisitEnumDeclaration(node);
    }

    public override void VisitDelegateDeclaration(DelegateDeclarationSyntax node)
    {
        AddType(node.Identifier);
        base.VisitDelegateDeclaration(node);
    }

    public override void VisitEnumMemberDeclaration(EnumMemberDeclarationSyntax node)
    {
        EnumMembers.Add(node.Identifier.ValueText);
        base.VisitEnumMemberDeclaration(node);
    }

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        string name = node.Identifier.ValueText;
        AllDeclaredNames.Add(name);
        if (Naming.IsImport(node) && Naming.SnakeCase.IsMatch(name))
        {
            ImportedFunctions.Add(name);
        }

        base.VisitMethodDeclaration(node);
    }

    public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        if (node.Parent is not TypeDeclarationSyntax typeDecl || !typeDecl.Identifier.ValueText.StartsWith("_"))
        {
            string name = node.Identifier.ValueText;
            if (Naming.SnakeCase.IsMatch(name))
            {
                SnakeCaseProperties.Add(name);
            }
        }

        base.VisitPropertyDeclaration(node);
    }

    public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
    {
        if (!node.Modifiers.Any(SyntaxKind.ConstKeyword) &&
            (node.Parent is not TypeDeclarationSyntax typeDecl || !typeDecl.Identifier.ValueText.StartsWith("_")))
        {
            foreach (VariableDeclaratorSyntax variable in node.Declaration.Variables)
            {
                string name = variable.Identifier.ValueText;
                if (Naming.SnakeCase.IsMatch(name))
                {
                    SnakeCaseProperties.Add(name);
                }
            }
        }

        base.VisitFieldDeclaration(node);
    }

    private void AddType(SyntaxToken identifier)
    {
        string name = identifier.ValueText;
        AllDeclaredNames.Add(name);
        if (Naming.SnakeCase.IsMatch(name))
        {
            SnakeCaseTypes.Add(name);
        }
    }
}