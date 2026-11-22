using System;
using System.Collections.Generic;
using Git2Sharp.Bindings;
using Git2Sharp.Common;
using Git2Sharp.Extensions;

namespace Git2Sharp;

/// <summary>
/// Represents a commit in a repository.
/// </summary>
public sealed unsafe class Commit : IDisposable
{
    private readonly NativeHandle<GitCommit> _handle;

    internal NativeHandle<GitCommit> Handle => _handle;

    /// <summary>
    /// The repository this branch belongs to.
    /// </summary>
    public Repository Repository { get; }

    /// <summary>
    /// The oid of this commit.
    /// </summary>
    public ObjectId Id
    {
        get
        {
            if (field is not null)
            {
                return field;
            }

            using NativeHandleLease<GitCommit> commit = _handle.Acquire();

            GitOid* id = Native.GitCommitId(commit.Object);

            return field = new ObjectId(id->Id);
        }
    }

    /// <summary>
    /// The summary text of this commit.
    /// </summary>
    public string Summary
    {
        get
        {
            using NativeHandleLease<GitCommit> commit = _handle.Acquire();

            InteropString summary = Native.GitCommitSummary(commit.Object);
            return summary;
        }
    }

    /// <summary>
    /// The body text of this commit.
    /// </summary>
    public string? Body
    {
        get
        {
            using NativeHandleLease<GitCommit> commit = _handle.Acquire();

            InteropString? body = InteropString.FromNative(Native.GitCommitBody(commit.Object));
            return body?.ToString();
        }
    }

    /// <summary>
    /// The author of thi commit.
    /// </summary>
    public Signature Author
    {
        get
        {
            using NativeHandleLease<GitCommit> commit = _handle.Acquire();

            GitSignature* signature = Native.GitCommitAuthor(commit.Object);

            return new Signature
            {
                Name = (InteropString)signature->Name,
                Email = (InteropString)signature->Email,
                When = signature->When.ToDateTime()
            };
        }
    }

    /// <summary>
    /// Resolves the parent commit.
    /// </summary>
    /// <remarks>
    /// If the commit has multiple parents e.g when it is a merge-commit,
    /// then commit with the parent idx zero is returned.
    /// </remarks>
    /// <exception cref="GitException">
    /// When the current commit don't have a parent commit.
    /// </exception>
    public Commit Parent
    {
        get
        {
            using NativeHandleLease<GitCommit> commit = _handle.Acquire();

            GitCommit* parent;
            int error = Native.GitCommitParent(&parent, commit.Object, 0);
            GitException.ThrowIfError(error);

            return new Commit(Repository, parent);
        }
    }

    /// <summary>
    /// Returns all parent commits of this commit
    /// </summary>
    /// <exception cref="GitException">
    /// When the current commit don't have any parent commits.
    /// </exception>
    public NativeCollection<Commit> Parents
    {
        get
        {
            using NativeHandleLease<GitCommit> commit = _handle.Acquire();

            uint count = Native.GitCommitParentcount(commit.Object);
            List<Commit> parents = new List<Commit>();

            try
            {

                for (uint i = 0; i < count; i++)
                {
                    GitCommit* parent;
                    int error = Native.GitCommitParent(&parent, commit.Object, i);
                    GitException.ThrowIfError(error);
                    parents.Add(new Commit(Repository, parent));
                }
            }
            catch
            {
                foreach (Commit parent in parents)
                {
                    parent.Dispose();
                }

                throw;
            }

            return new NativeCollection<Commit>(parents);
        }
    }

    internal Commit(Repository repository, GitCommit* commit)
    {
        Repository = repository;

        _handle = new NativeHandle<GitCommit>(
            commit,
            static ptr => Native.GitCommitFree((GitCommit*)ptr)
        );
    }

    /// <summary>
    /// Releases the native resources associated with this commit.
    /// </summary>
    public void Dispose()
    {
        _handle.Dispose();
    }
}