using System;

namespace Git2Sharp;

public sealed record Signature
{
    public required string Name { get; init; }
    public required string Email { get; init; }
    public required DateTime When { get; init; }
}