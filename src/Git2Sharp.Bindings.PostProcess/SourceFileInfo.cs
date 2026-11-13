using Microsoft.CodeAnalysis;

namespace Git2Sharp.Bindings.PostProcess;

public record SourceFileInfo
{
    public required string Path { get; init; }
    public required string Text { get; init; }
    public required SyntaxTree Tree { get; init; }
}