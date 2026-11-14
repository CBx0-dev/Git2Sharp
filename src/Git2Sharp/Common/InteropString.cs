using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Git2Sharp.Common;

/// <summary>
/// Represents a string that can be converted between managed and native UTF-8 representation.
/// </summary>
/// <remarks>
/// The managed string is used as the primary representation. A native UTF-8 buffer is allocated
/// lazily and only once when requested and released when the instance is collected.
/// </remarks>
public sealed unsafe class InteropString
{
    private readonly Lock _lock;
    private readonly string _str;

    private sbyte* _buf;

    internal InteropString(sbyte* buf)
    {
        ArgumentNullException.ThrowIfNull(buf);

        _lock = new Lock();
        _str = new string(buf);
        _buf = null;
    }

    internal InteropString(string str)
    {
        ArgumentNullException.ThrowIfNull(str);

        _lock = new Lock();
        _buf = null;
        _str = str;
    }

    ~InteropString()
    {
        if (_buf is not null)
        {
            Marshal.FreeCoTaskMem((IntPtr)_buf);
            _buf = null;
        }
    }

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
    /// The returned pointer is owned by this instance and remains valid while the instance is alive.
    /// The pointer must not be freed by the caller.
    ///
    /// Calling this method twice will return the same pointer.
    /// </remarks>
    public sbyte* ToBuffer()
    {
        lock (_lock)
        {
            if (_buf is not null)
            {
                return _buf;
            }

            _buf = (sbyte*)Marshal.StringToCoTaskMemUTF8(_str);

            return _buf;
        }
    }

    public static implicit operator string(InteropString str) => str.ToString();

    public static implicit operator sbyte*(InteropString str) => str.ToBuffer();

    public static implicit operator InteropString(string str) => new InteropString(str);

    public static implicit operator InteropString(sbyte* buf) => new InteropString(buf);
}