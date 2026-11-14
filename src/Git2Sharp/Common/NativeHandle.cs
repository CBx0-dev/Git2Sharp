using System;
using System.Threading;

namespace Git2Sharp.Common;

/// <summary>
/// Represents an owning handle to an umanaged object and manages its lifetime while operations are using it.
/// </summary>
/// <typeparam name="T">The unmanaged type pointed to by the native handle.</typeparam>
internal sealed unsafe class NativeHandle<T> : IDisposable where T : unmanaged
{
    private readonly Lock _lock;
    private readonly Action<IntPtr> _release;

    private IntPtr _ptr;
    private int _activeOperations;
    private bool _disposeRequested;

    /// <summary>
    /// Gets a value indicating whether the native handle has been disposed.
    /// </summary>
    /// <remarks>
    /// A handle is considered disposed once its native pointer has been released.
    /// </remarks>
    public bool IsDisposed => Volatile.Read(ref _ptr) == IntPtr.Zero;

    internal NativeHandle(T* ptr, Action<IntPtr> release)
    {
        ArgumentNullException.ThrowIfNull(ptr);

        _lock = new Lock();
        _release = release;
        _ptr = (IntPtr)ptr;
        _activeOperations = 0;
        _disposeRequested = false;
    }

    ~NativeHandle()
    {
        IntPtr ptr = Interlocked.Exchange(ref _ptr, IntPtr.Zero);
        if (ptr != IntPtr.Zero)
        {
            _release(ptr);
        }
    }

    /// <summary>
    /// Acquires a lease on the native object, preventing its release until the lease is disposed.
    /// </summary>
    /// <returns>A lease providing access to the native object.</returns>
    /// <exception cref="ObjectDisposedException">
    /// Then handle has already been disposed or disposal has been requested.
    /// </exception>
    public NativeHandleLease<T> Acquire()
    {
        lock (_lock)
        {
            if (_disposeRequested)
            {
                throw new ObjectDisposedException(nameof(NativeHandle<T>));
            }

            _activeOperations++;

            return new NativeHandleLease<T>(this, (T*)_ptr);
        }
    }

    /// <summary>
    /// Requests disposal of the native handle.
    /// </summary>
    /// <remarks>
    /// If there are active leases, the native object is released when the last lease is disposed.
    /// Otherwise, it is released immediately.
    ///
    /// Subsequent calls have no effect.
    /// </remarks>
    public void Dispose()
    {
        IntPtr ptr = IntPtr.Zero;

        lock (_lock)
        {
            if (_disposeRequested)
            {
                return;
            }

            _disposeRequested = true;
            if (_activeOperations == 0)
            {
                ptr = _ptr;
                _ptr = IntPtr.Zero;
            }
        }

        if (ptr != IntPtr.Zero)
        {
            _release(ptr);
            GC.SuppressFinalize(this);
        }
    }

    internal void Release()
    {
        IntPtr ptr = IntPtr.Zero;

        lock (_lock)
        {
            _activeOperations--;

            if (_disposeRequested && _activeOperations == 0)
            {
                ptr = _ptr;
                _ptr = IntPtr.Zero;
            }
        }

        if (ptr != IntPtr.Zero)
        {
            _release(ptr);
            GC.SuppressFinalize(this);
        }
    }
}