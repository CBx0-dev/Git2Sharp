using System;
using Git2Sharp.Bindings;
using Git2Sharp.Common;

namespace Git2Sharp;

/// <summary>
/// Represents a tag in a repository.
/// </summary>
public sealed unsafe class Tag : IDisposable
{
    private readonly NativeHandle<GitTag> _handle;

    internal NativeHandle<GitTag> Handle => _handle;

    /// <summary>
    /// The repository this tag belongs to.
    /// </summary>
    public Repository Repository { get; }

    /// <summary>
    /// Gets the name of the tag.
    /// </summary>
    /// <remarks>
    /// The name is retrieved from the underlying native tag
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

            using NativeHandleLease<GitTag> tag = _handle.Acquire();

            InteropString name = Native.GitTagName(tag.Object);
            return field = name;
        }
    }

    /// <summary>
    /// Resolves the commit pointed to by this tag.
    /// </summary>
    /// <exception cref="GitException">
    /// When an error occurs while peeling or resolving the target commit.
    /// </exception>
    public Commit Commit
    {
        get
        {
            using NativeHandleLease<GitTag> tag = _handle.Acquire();
            GitObject* target;

            int error = Native.GitTagTarget(
                &target,
                tag.Object
            );
            GitException.ThrowIfError(error);

            GitCommit* commit;
            error = Native.GitObjectPeel(
                (GitObject**)&commit,
                target,
                GitObjectT.GIT_OBJECT_COMMIT
            );
            GitException.ThrowIfError(error);

            Native.GitObjectFree(target);

            return new Commit(Repository, commit);
        }
    }

    internal Tag(Repository repository, GitTag* tag)
    {
        Repository = repository;

        _handle = new NativeHandle<GitTag>(
            tag,
            static ptr => Native.GitTagFree((GitTag*)ptr)
        );
    }

    /// <summary>
    /// Releases the native resources associated with this tag.
    /// </summary>
    public void Dispose()
    {
        _handle.Dispose();
    }
}