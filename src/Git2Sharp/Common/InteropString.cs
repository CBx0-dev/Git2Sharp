using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Git2Sharp.Common;

/// <summary>
/// Represents a string that can be converted between managed and native UTF-8 representation.
/// </summary>
/// <remarks>
/// The managed string is the primary representation and is immutable. Instances created from a native
/// pointer copy the data immediately, so they do not depend on the lifetime of the native memory.
///
/// A null-terminated UTF-8 buffer is created lazily, and only once, when <see cref="ToBuffer"/> is first
/// requested. The buffer is a pinned array on the GC's pinned object heap: it needs no finalizer and no
/// manual release, and the GC reclaims it after the instance has become unreachable.
///
/// Instances compare by content (ordinal). The type deliberately defines no <c>==</c> operator: with
/// implicit conversions in both directions between <see cref="string"/> and <see cref="InteropString"/>,
/// such an operator would make comparisons like <c>x == null</c> ambiguous. Use <see cref="Equals(InteropString?)"/>
/// or convert to <see cref="string"/>.
/// </remarks>
internal sealed unsafe class InteropString : IEquatable<InteropString>, IEquatable<string>
{
    private readonly string _str;

    private byte[]? _buf;

    private InteropString(sbyte* buf)
    {
        ArgumentNullException.ThrowIfNull(buf);

        _str = Marshal.PtrToStringUTF8((IntPtr)buf)!;
        _buf = null;
    }

    private InteropString(string str)
    {
        ArgumentNullException.ThrowIfNull(str);

        // C would silently stop reading at the first NUL, so "a\0b" would reach libgit2 as "a".
        if (str.Contains('\0'))
        {
            throw new ArgumentException("The string must not contain NUL characters.", nameof(str));
        }

        _str = str;
        _buf = null;
    }

    /// <summary>
    /// Creates an <see cref="InteropString"/> from a native, null-terminated UTF-8 string.
    /// </summary>
    /// <param name="buf">The native string, or <see langword="null"/>.</param>
    /// <returns>
    /// A copy of the native string, or <see langword="null"/> when <paramref name="buf"/> is <see langword="null"/>.
    /// </returns>
    /// <remarks>
    /// Use this for optional libgit2 strings. The implicit conversion from <c>sbyte*</c> throws for a null pointer.
    /// </remarks>
    internal static InteropString? FromNative(sbyte* buf) => buf is null
        ? null
        : new InteropString(buf);

    /// <summary>
    /// Gets a managed string representation.
    /// </summary>
    /// <returns>The managed string.</returns>
    public override string ToString() => _str;

    /// <summary>
    /// Gets a pointer to the null-terminated UTF-8 representation.
    /// </summary>
    /// <returns>A pointer to the UTF-8 buffer.</returns>
    /// <remarks>
    /// The pointer addresses a pinned array owned by this instance. The memory must not be modified or freed
    /// by the caller.
    ///
    /// Calling this method repeatedly returns the same pointer.
    ///
    /// The pointer is valid only while this instance is reachable. The GC does not know that native code is
    /// using it, so keep the instance alive until the native call has returned, for example with
    /// <see cref="GC.KeepAlive(object?)"/> after the call.
    /// </remarks>
    public sbyte* ToBuffer()
    {
        byte[]? buf = Volatile.Read(ref _buf);
        if (buf is null)
        {
            byte[] created = CreatePinnedBuffer(_str);

            // If another thread was faster, its buffer wins and ours is simply garbage.
            buf = Interlocked.CompareExchange(ref _buf, created, null) ?? created;
        }

        return (sbyte*)Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(buf));
    }

    private static byte[] CreatePinnedBuffer(string str)
    {
        int byteCount = Encoding.UTF8.GetByteCount(str);

        // The array is zero-initialized, so the extra last byte is the NUL terminator.
        byte[] buffer = GC.AllocateArray<byte>(byteCount + 1, pinned: true);
        Encoding.UTF8.GetBytes(str, 0, str.Length, buffer, 0);

        return buffer;
    }

    /// <summary>
    /// Determines whether this instance has the same content as another <see cref="InteropString"/> (ordinal).
    /// </summary>
    public bool Equals(InteropString? other) =>
        other is not null && string.Equals(_str, other._str, StringComparison.Ordinal);

    /// <summary>
    /// Determines whether this instance has the same content as a <see cref="string"/> (ordinal).
    /// </summary>
    public bool Equals(string? other) => string.Equals(_str, other, StringComparison.Ordinal);

    public override bool Equals(object? obj) => obj is InteropString other && Equals(other);

    public override int GetHashCode() => _str.GetHashCode();

    public static implicit operator string(InteropString str) => str.ToString();

    public static implicit operator sbyte*(InteropString str) => str.ToBuffer();

    public static implicit operator InteropString(string str) => new InteropString(str);

    public static implicit operator InteropString(sbyte* buf) => new InteropString(buf);
}