using System;
using System.Collections.Generic;
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
    public NativeCollection<Branch> All => CollectBranches(GitBranchT.GIT_BRANCH_ALL);

    /// <summary>
    /// Gets all local branches in the repository.
    /// </summary>
    /// <returns>A collection containing all local branches in the repository</returns>
    public NativeCollection<Branch> Local => CollectBranches(GitBranchT.GIT_BRANCH_LOCAL);

    /// <summary>
    /// Gets all remote branches in the repository.
    /// </summary>
    /// <returns>A collection containing all remote branches in the repository</returns>
    public NativeCollection<Branch> Remote => CollectBranches(GitBranchT.GIT_BRANCH_REMOTE);

    internal BranchHandler(Repository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Gets a branch by its name.
    /// </summary>
    /// <param name="name">The name of the branch to retrieve</param>
    /// <param name="type">The type of branch to look up.</param>
    /// <returns>The matching branch</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="type"/> is <see cref="GitBranchT.GIT_BRANCH_ALL"/>
    /// </exception>
    /// <exception cref="GitException">
    /// The branch could not be found, its name is malformed or another error occoured. 
    /// </exception>
    public Branch GetBranch(string name, GitBranchT type) => GetBranch((InteropString)name, type);

    private Branch GetBranch(InteropString name, GitBranchT type)
    {
        if (type == GitBranchT.GIT_BRANCH_ALL)
        {
            throw new ArgumentException("Using 'GitBranchT.GIT_BRANCH_ALL' is not allowed in this context", nameof(type));
        }

        using NativeHandleLease<GitRepository> repository = _repository.Handle.Acquire();
        GitReference* reference = null;

        int error = Native.GitBranchLookup(&reference, repository.Object, name, type);
        GitException.ThrowIfError(error);

        return new Branch(_repository, reference, type);
    }

    /// <summary>
    /// Gets a branch by its name, or <see langword="null"/> if it does not exist.
    /// </summary>
    /// <param name="name">The name of the branch to retrieve</param>
    /// <param name="type">The type of branch to look up.</param>
    /// <returns>The matching branch, or <see langword="null"/> if no matching branch was exists.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="type"/> is <see cref="GitBranchT.GIT_BRANCH_ALL"/>
    /// </exception>
    /// <exception cref="GitException">
    /// The branch name is malformed or another error occurred.
    /// </exception>
    public Branch? GetBranchOrDefault(string name, GitBranchT type) => GetBranchOrDefault((InteropString)name, type);

    private Branch? GetBranchOrDefault(InteropString name, GitBranchT type)
    {
        if (type == GitBranchT.GIT_BRANCH_ALL)
        {
            throw new ArgumentException("Using 'GitBranchT.GIT_BRANCH_ALL' is not allowed in this context", nameof(type));
        }

        using NativeHandleLease<GitRepository> repository = _repository.Handle.Acquire();
        GitReference* reference = null;

        int error = Native.GitBranchLookup(&reference, repository.Object, name, type);
        if (error == (int)GitErrorCode.GIT_ENOTFOUND)
        {
            return null;
        }

        GitException.ThrowIfError(error);

        return new Branch(_repository, reference, type);
    }

    private NativeCollection<Branch> CollectBranches(GitBranchT expectedType)
    {
        using NativeHandleLease<GitRepository> repository = _repository.Handle.Acquire();

        using BranchIterator iterator = new BranchIterator(repository.Object, expectedType);

        List<Branch> branches = new List<Branch>();

        try
        {
            while (iterator.MoveNext(out GitReference* reference, out GitBranchT branchType))
            {
                branches.Add(new Branch(_repository, reference, branchType));
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

        return new NativeCollection<Branch>(branches);
    }
}