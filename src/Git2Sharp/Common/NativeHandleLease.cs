using System;
using System.Threading;

namespace Git2Sharp.Common;

/// <summary>
/// Represents a temporary lease on a native object, ensuring that the underlying
/// native handle remains valid for the duration of the lease.
/// </summary>
/// <typeparam name="T">The unmanaged type pointed to by the native handle</typeparam>
internal sealed unsafe class NativeHandleLease<T> : IDisposable where T : unmanaged
{
    private NativeHandle<T>? _owner;

    /// <summary>
    /// Gets a pointer to the underlying native object.
    /// </summary>
    /// <remarks>
    /// The pointer is valid for the lifetime of this lease.
    /// It must not be used after the lease has been disposed.
    /// </remarks>
    public T* Object { get; }

    internal NativeHandleLease(NativeHandle<T> owner, T* ptr)
    {
        ArgumentNullException.ThrowIfNull(ptr);

        _owner = owner;
        Object = ptr;
    }

    /// <summary>
    /// Releases the lease and allows the owning native handle to release
    /// the underlying native object when no other lease remain.
    /// </summary>
    /// <remarks>
    /// Calling this method multiple times has no additional effect.
    /// </remarks>
    public void Dispose()
    {
        NativeHandle<T>? owner = Interlocked.Exchange(ref _owner, null);

        if (owner is not null)
        {
            owner.Release();
        }
    }
}