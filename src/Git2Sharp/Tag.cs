using System;

namespace Git2Sharp;

public sealed class Tag : IDisposable
{
    public string Name { get; }

    internal Tag(string name)
    {
        Name = name;
    }

    public void Dispose()
    {
        // Doing nothing, because we need to satisfy
        // the NativeCollection<T> contract, but we
        // don't holding any native resources.
    }
}