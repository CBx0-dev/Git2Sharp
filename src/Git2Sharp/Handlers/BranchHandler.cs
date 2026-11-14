using System.Collections.Generic;
using System.Collections.Immutable;
using Git2Sharp.Bindings;
using Git2Sharp.Common;

namespace Git2Sharp.Handlers;

/// <summary>
/// Provides access to branches in a repository.
/// </summary>
public sealed unsafe class BranchHandler
{
    private readonly Repository _repository;

    /// <summary>
    /// Gets all local and remote branches in the repository.
    /// </summary>
    /// <returns>A collection containing all branches in the repository.</returns>
    public BranchCollection All => CollectBranches(GitBranchT.GIT_BRANCH_ALL);

    /// <summary>
    /// Gets all local branches in the repository.
    /// </summary>
    /// <returns>A collection containing all local branches in the repository</returns>
    public BranchCollection Local => CollectBranches(GitBranchT.GIT_BRANCH_LOCAL);

    /// <summary>
    /// Gets all remote branches in the repository.
    /// </summary>
    /// <returns>A collection containing all remote branches in the repository</returns>
    public BranchCollection Remote => CollectBranches(GitBranchT.GIT_BRANCH_REMOTE);

    internal BranchHandler(Repository repository)
    {
        _repository = repository;
    }

    private BranchCollection CollectBranches(GitBranchT expectedType)
    {
        using NativeHandleLease<GitRepository> repository = _repository.Handle.Acquire();

        using BranchIterator iterator = new BranchIterator(repository.Object, expectedType);

        List<Branch> branches = new List<Branch>();

        try
        {
            while (iterator.MoveNext(out GitReference* reference, out GitBranchT branchType))
            {
                branches.Add(new Branch(reference, branchType));
            }
        }
        catch
        {
            foreach (Branch branch in branches)
            {
                branch.Dispose();
            }

            throw;
        }

        return new BranchCollection(branches.ToImmutableList());
    }
}