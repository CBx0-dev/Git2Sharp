using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace Git2Sharp.Common;

/// <summary>
/// Represents a collection of disposable native-backed objects.
/// </summary>
/// <typeparam name="TItem">
/// The type of objects contained in the collection.
/// </typeparam>
/// <remarks>
/// The collection owns the objects it contains and is responsible for
/// disposing them when the collection itself is disposed.
/// 
/// Objects obtained from the collection should not be used after the
/// collection has been disposed.
/// </remarks>
public sealed class NativeCollection<TItem> : IReadOnlyList<TItem>, IDisposable
    where TItem : class, IDisposable
{
    private readonly ImmutableList<TItem> _items;
    private bool _disposed;

    internal NativeCollection(IEnumerable<TItem> items)
    {
        _items = items.ToImmutableList();
        _disposed = false;
    }

    public int Count => _items.Count;

    public TItem this[int index] => _items[index];

    public IEnumerator<TItem> GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Releases all resources owned by the collection.
    /// </summary>
    /// <remarks>
    /// Disposes all objects contained in the collection.
    /// Subsequent calls have no effect.
    /// </remarks>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, true))
        {
            return;
        }

        foreach (TItem branch in _items)
        {
            branch.Dispose();
        }
    }
}