using System;
using System.Diagnostics.CodeAnalysis;
using Git2Sharp.Bindings;

namespace Git2Sharp;

/// <summary>
/// An OID that determines uniquely a commit, ref
/// and other things in git
/// </summary>
public sealed record ObjectId
{
    /// <summary>
    /// The raw bytes of the OID
    /// </summary>
    public byte[] Bytes { get; }

    /// <summary>
    /// The SHA-1 representation of the OID
    /// </summary>
    public string Sha => Convert.ToHexString(Bytes);

    public ObjectId(byte[] bytes)
    {
        if (bytes.Length != 20)
        {
            throw new ArgumentOutOfRangeException(nameof(bytes), "Array must be of size 20");
        }

        Bytes = bytes;
    }

    internal ObjectId(GitOid._id_e__FixedBuffer oid)
    {

        byte[] bytes = new byte[20];
        ReadOnlySpan<byte> view = oid;
        view.CopyTo(bytes);

        Bytes = bytes;
    }

    /// <summary>
    /// Parses a SHA-1 hash and returns a <see cref="ObjectId"/> instance.
    /// </summary>
    /// <param name="oid">The SHA-1 string of OID</param>
    /// <returns>
    /// A instance of the <see cref="ObjectId"/> containing the parsed hash.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// When the hash is invalid and dont resolve to 20 bytes.
    /// </exception>
    public static ObjectId Parse(string oid)
    {
        ArgumentNullException.ThrowIfNull(oid);
        try
        {
            byte[] bytes = Convert.FromHexString(oid);
            if (bytes.Length != 20)
            {
                throw new ArgumentException("Hex string must resolve to 20 bytes (40 characters).", nameof(oid));
            }

            return new ObjectId(bytes);
        }
        catch (FormatException ex)
        {
            throw new ArgumentException("Invalid hex string.", nameof(oid), ex);
        }
    }

    /// <summary>
    /// Try parses an SHA-1 hash.
    /// </summary>
    /// <param name="id">
    /// Contains the parsed hash or <see langword="null"/> when parsing failed.
    /// </param>
    /// <param name="oid">
    /// The hash that should be tried to be parsed.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if parsing succeeded, otherwise <see langword="false"/>
    /// </returns>
    public static bool TryParse([NotNullWhen(true)] out ObjectId? id, string? oid)
    {
        if (oid is null)
        {
            id = null;
            return false;
        }

        try
        {
            byte[] bytes = Convert.FromHexString(oid);
            if (bytes.Length != 20)
            {
                id = null;
                return false;
            }

            id = new ObjectId(bytes);
            return true;
        }
        catch (FormatException)
        {
            id = null;
            return false;
        }
    }

    /// <summary>
    /// Determines whether this <see cref="ObjectId"/> equals another <see cref="ObjectId"/>.
    /// </summary>
    public bool Equals(ObjectId? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Bytes.AsSpan().SequenceEqual(other.Bytes);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        HashCode hash = new HashCode();
        hash.AddBytes(Bytes);
        return hash.ToHashCode();
    }

    internal GitOid ToOid()
    {
        GitOid oid = new GitOid();
        Bytes.AsSpan().CopyTo(oid.Id);

        return oid;
    }
}