using System;
using Git2Sharp.Common;

namespace Git2Sharp;

public sealed class Tag : IDisposable
{
    public InteropString Name { get; }

    public Tag(InteropString name)
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