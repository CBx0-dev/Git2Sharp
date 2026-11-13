using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Git2Sharp.Bindings.PostProcess;

public sealed class Rewriter : CSharpSyntaxRewriter
{
    private readonly Dictionary<string, string> _renames;
    private readonly HashSet<string> _importedFunctions;
    private readonly HashSet<string> _enumMembers;

    public int ConstCasts { get; private set; }

    public Rewriter(Dictionary<string, string> renames, HashSet<string> importedFunctions, HashSet<string> enumMembers)
    {
        _renames = renames;
        _importedFunctions = importedFunctions;
        _enumMembers = enumMembers;
    }

    // Renames every identifier that refers to a renamed type or function. String literals
    // (for example [NativeTypeName("git_oid *")]) are not identifiers and stay untouched.
    public override SyntaxToken VisitToken(SyntaxToken token)
    {
        if (token.IsKind(SyntaxKind.IdentifierToken) && _renames.TryGetValue(token.ValueText, out string? newName))
        {
            return SyntaxFactory.Identifier(token.LeadingTrivia, newName, token.TrailingTrivia);
        }

        return base.VisitToken(token);
    }

    // Keeps the native symbol name on renamed P/Invoke functions.
    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        string originalName = node.Identifier.ValueText;
        MethodDeclarationSyntax visited = (MethodDeclarationSyntax)base.VisitMethodDeclaration(node)!;
        if (!_importedFunctions.Contains(originalName))
        {
            return visited;
        }

        List<AttributeSyntax> importAttributes = visited.AttributeLists
            .SelectMany(list => list.Attributes)
            .Where(Naming.IsImportAttribute)
            .ToList();

        return visited.ReplaceNodes(importAttributes, (attribute, _) => WithEntryPoint(attribute, originalName));
    }


    public override SyntaxNode? VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        PropertyDeclarationSyntax visited = (PropertyDeclarationSyntax)base.VisitPropertyDeclaration(node)!;
        if (node.Parent is TypeDeclarationSyntax typeDecl && typeDecl.Identifier.ValueText.StartsWith("_"))
        {
            return visited;
        }

        string name = visited.Identifier.ValueText;
        if (Naming.SnakeCase.IsMatch(name))
        {
            SyntaxToken newIdentifier = SyntaxFactory.Identifier(
                visited.Identifier.LeadingTrivia,
                Naming.ToPascalCase(name),
                visited.Identifier.TrailingTrivia);
            return visited.WithIdentifier(newIdentifier);
        }

        return visited;
    }

    // const int GIT_ATTR_TRUE_T = GIT_ATTR_VALUE_TRUE;  ->  = (int)GIT_ATTR_VALUE_TRUE;
    public override SyntaxNode? VisitFieldDeclaration(FieldDeclarationSyntax node)
    {
        FieldDeclarationSyntax visited = (FieldDeclarationSyntax)base.VisitFieldDeclaration(node)!;
        if (!visited.Modifiers.Any(SyntaxKind.ConstKeyword))
        {
            if (node.Parent is TypeDeclarationSyntax typeDecl && typeDecl.Identifier.ValueText.StartsWith("_"))
            {
                return visited;
            }

            SeparatedSyntaxList<VariableDeclaratorSyntax> newVariables = SyntaxFactory.SeparatedList(
                visited.Declaration.Variables.Select(variable =>
                {
                    string name = variable.Identifier.ValueText;
                    if (Naming.SnakeCase.IsMatch(name))
                    {
                        SyntaxToken newIdentifier = SyntaxFactory.Identifier(
                            variable.Identifier.LeadingTrivia,
                            Naming.ToPascalCase(name),
                            variable.Identifier.TrailingTrivia);
                        return variable.WithIdentifier(newIdentifier);
                    }

                    return variable;
                }));

            return visited.WithDeclaration(visited.Declaration.WithVariables(newVariables));
        }

        if (visited.Declaration.Type is not PredefinedTypeSyntax)
        {
            return visited;
        }

        TypeSyntax constType = visited.Declaration.Type.WithoutTrivia();
        VariableDeclarationSyntax declaration = visited.Declaration;
        foreach (VariableDeclaratorSyntax variable in visited.Declaration.Variables)
        {
            if (variable.Initializer is null)
            {
                continue;
            }

            ExpressionSyntax value = variable.Initializer.Value;
            List<ExpressionSyntax> targets = new List<ExpressionSyntax>();
            foreach (IdentifierNameSyntax identifier in value.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>())
            {
                if (!_enumMembers.Contains(identifier.Identifier.ValueText))
                {
                    continue;
                }

                // Qualified use (SomeEnum.Member): cast the whole member access.
                ExpressionSyntax target = identifier.Parent is MemberAccessExpressionSyntax access && access.Name == identifier
                    ? access
                    : identifier;

                if (target.Parent is not CastExpressionSyntax && !targets.Contains(target))
                {
                    targets.Add(target);
                }
            }

            if (targets.Count == 0)
            {
                continue;
            }

            ExpressionSyntax newValue = value.ReplaceNodes(
                targets,
                (original, _) => SyntaxFactory.CastExpression(constType, original.WithoutTrivia()).WithTriviaFrom(original));
            ConstCasts += targets.Count;

            VariableDeclaratorSyntax newVariable = variable.WithInitializer(variable.Initializer.WithValue(newValue));
            declaration = declaration.ReplaceNode(
                declaration.Variables.First(v => v.Identifier.ValueText == variable.Identifier.ValueText),
                newVariable);
        }

        return visited.WithDeclaration(declaration);
    }

    private static AttributeSyntax WithEntryPoint(AttributeSyntax attribute, string nativeName)
    {
        bool existing = attribute.ArgumentList?.Arguments
                            .Any(argument => argument.NameEquals?.Name.Identifier.ValueText == "EntryPoint") ??
                        false;
        if (existing)
        {
            return attribute;
        }

        AttributeArgumentSyntax entryPoint = SyntaxFactory.AttributeArgument(
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal(nativeName)))
            .WithNameEquals(SyntaxFactory.NameEquals(SyntaxFactory.IdentifierName("EntryPoint")));

        return attribute.ArgumentList is null
            ? attribute.WithArgumentList(
                SyntaxFactory.AttributeArgumentList(SyntaxFactory.SingletonSeparatedList(entryPoint)))
            : attribute.WithArgumentList(attribute.ArgumentList.AddArguments(entryPoint));
    }
}