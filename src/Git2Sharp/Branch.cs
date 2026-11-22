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
    /// The repository this branch belongs to.
    /// </summary>
    public Repository Repository { get; }

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
    public string Name
    {
        get
        {
            if (field is not null)
            {
                return field;
            }

            using NativeHandleLease<GitReference> reference = _handle.Acquire();

            field = (InteropString)Native.GitReferenceShorthand(reference.Object);
            return field;
        }
    }

    /// <summary>
    /// Resolves the HEAD Commit of the Branch.
    /// </summary>
    /// <exception cref="GitException">
    /// If the Branch dont contains any commits.
    /// </exception>
    public Commit Head
    {
        get
        {
            GitCommit* obj;
            
            using NativeHandleLease<GitReference> reference = _handle.Acquire();

            int error = Native.GitReferencePeel(
                (GitObject**)&obj,
                reference.Object,
                GitObjectT.GIT_OBJECT_COMMIT    
            );
            GitException.ThrowIfError(error);

            return new Commit(Repository, obj);
        }
    }

    internal Branch(Repository repository, GitReference* reference, GitBranchT type)
    {
        _handle = new NativeHandle<GitReference>(
            reference,
            static ptr => Native.GitReferenceFree((GitReference*)ptr)
        );
        _type = type;

        Repository = repository;
    }


    /// <summary>
    /// Releases the native resources associated with this branch.
    /// </summary>
    public void Dispose()
    {
        _handle.Dispose();
    }
}