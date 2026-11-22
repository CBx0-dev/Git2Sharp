using Git2Sharp.Bindings;
using Git2Sharp.Common;

namespace Git2Sharp.Handlers;


/// <summary>
/// Provides access to commits in a repository.
/// </summary>
public sealed unsafe class CommitHandler
{
    private readonly Repository _repository;

    internal CommitHandler(Repository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Resolves a commit by its Oid
    /// </summary>
    /// <param name="objectId">The Oid if the commit</param>
    /// <returns>The resolved commit</returns>
    /// <exception cref="GitException">
    /// When the commit dont exists.
    /// </exception>
    public Commit GetCommit(string hash) => GetCommit(ObjectId.Parse(hash));

    /// <summary>
    /// Resolves a commit by its Oid
    /// </summary>
    /// <param name="objectId">The Oid if the commit</param>
    /// <returns>The resolved commit</returns>
    /// <exception cref="GitException">
    /// When the commit dont exists.
    /// </exception>
    public Commit GetCommit(ObjectId objectId)
    {
        using NativeHandleLease<GitRepository> repository = _repository.Handle.Acquire();

        GitOid oid = objectId.ToOid();
        GitCommit* commit;

        int error = Native.GitCommitLookup(
            &commit,
            repository.Object,
            &oid
        );
        GitException.ThrowIfError(error);

        return new Commit(_repository, commit);
    }

    /// <summary>
    /// Tries to resolve a commit by its Oid
    /// </summary>
    /// <param name="objectId">The Oid if the commit</param>
    /// <returns>The resolved commit or <see langword="null"/> if the commit don't exist</returns>
    /// <exception cref="GitException">
    /// When an error occurs during lookup
    /// </exception>
    public Commit? GetCommitOrDefault(string hash) => GetCommitOrDefault(ObjectId.Parse(hash));

    /// <summary>
    /// Tries to resolve a commit by its Oid
    /// </summary>
    /// <param name="objectId">The Oid if the commit</param>
    /// <returns>The resolved commit or <see langword="null"/> if the commit don't exist</returns>
    /// <exception cref="GitException">
    /// When an error occurs during lookup
    /// </exception>
    public Commit? GetCommitOrDefault(ObjectId objectId)
    {
        using NativeHandleLease<GitRepository> repository = _repository.Handle.Acquire();

        GitOid oid = objectId.ToOid();
        GitCommit* commit;

        int error = Native.GitCommitLookup(
            &commit,
            repository.Object,
            &oid
        );
        if (error == (int)GitErrorCode.GIT_ENOTFOUND)
        {
            return null;
        }

        GitException.ThrowIfError(error);

        return new Commit(_repository, commit);
    }
}