using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace Git2Sharp;

/// <summary>
/// Represents a collection of branches in a repository.
/// </summary>
/// <remarks>
/// The collection owns the <see cref="Branch"/> instances it contains.
/// Disposing the collection releases all associated native resources.
/// 
/// Branches should not be used after the collection has been disposed.
/// </remarks>
public sealed class BranchCollection : IReadOnlyList<Branch>, IDisposable
{
    private readonly ImmutableList<Branch> _branches;
    private bool _disposed;

    internal BranchCollection(ImmutableList<Branch> branches)
    {
        _branches = branches;
        _disposed = false;
    }

    public int Count => _branches.Count;

    public Branch this[int index] => _branches[index];

    public IEnumerator<Branch> GetEnumerator() => _branches.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Releases all associated native resources owned by the collection.
    /// </summary>
    /// <remarks>
    /// Disposes all branches contained in the collection.
    /// Subsequent calls have no effect.
    /// </remarks>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, true))
        {
            return;
        }

        foreach (Branch branch in _branches)
        {
            branch.Dispose();
        }
    }
}