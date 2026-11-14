using System;
using Git2Sharp.Bindings;
using Git2Sharp.Common;

namespace Git2Sharp;

/// <summary>
/// Represents a local or remote branch in a repository.
/// </summary>
public sealed unsafe class Branch : IDisposable
{
    private readonly NativeHandle<GitReference> _handle;
    private readonly GitBranchT _type;

    /// <summary>
    /// Gets a value indicating whether the branch is local.
    /// </summary>
    public bool IsLocal => ((uint)_type & (uint)GitBranchT.GIT_BRANCH_LOCAL) != 0;

    /// <summary>
    /// Gets a value indicating whether the branch is remote.
    /// </summary>
    public bool IsRemote => ((uint)_type & (uint)GitBranchT.GIT_BRANCH_REMOTE) != 0;

    /// <summary>
    /// Gets the shorthand name of the branch.
    /// </summary>
    /// <remarks>
    /// The name is retried from the underlying native reference
    /// on first access and cached for subsequent accesses.
    /// </remarks>
    public InteropString Name
    {
        get
        {
            if (field is not null)
            {
                return field;
            }

            using NativeHandleLease<GitReference> reference = _handle.Acquire();

            field = Native.GitReferenceShorthand(reference.Object);
            return field;
        }
    }

    internal Branch(GitReference* reference, GitBranchT type)
    {
        _handle = new NativeHandle<GitReference>(
            reference,
            static ptr => Native.GitReferenceFree((GitReference*)ptr)
        );
        _type = type;
    }


    /// <summary>
    /// Releases the native resources associated with this branch.
    /// </summary>
    public void Dispose()
    {
        _handle.Dispose();
    }
}