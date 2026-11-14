using System;
using Git2Sharp.Bindings;
using Git2Sharp.Common;
using Git2Sharp.Handlers;

namespace Git2Sharp;

/// <summary>
/// Represents a Git repository and provides access to its objects and references.
/// </summary>
/// <remarks>
/// A repository acts as the central entry point for interacting with Git.
/// It exposes specialized handlers for branches, commits, tags, and other
/// repository related operations.
/// </remarks>
public sealed unsafe class Repository : IDisposable
{
    private readonly NativeHandle<GitRepository> _handle;

    internal NativeHandle<GitRepository> Handle => _handle;

    /// <summary>
    /// Perform operations related to branches in this repository.
    /// </summary>
    public BranchHandler Branches { get; }

    /// <summary>
    /// Perform operations related to tags in this repository.
    /// </summary>
    public TagHandler Tags { get; }

    private Repository(GitRepository* repository)
    {
        ArgumentNullException.ThrowIfNull(repository);

        _handle = new NativeHandle<GitRepository>(
            repository,
            static ptr => Native.GitRepositoryFree((GitRepository*)ptr)
        );

        Branches = new BranchHandler(this);
        Tags = new TagHandler(this);
    }

    /// <summary>
    /// Releases the native resources associated with this repository.
    /// </summary>
    public void Dispose()
    {
        _handle.Dispose();
    }

    /// <summary>
    /// Opens an existing Git repository at the specified path
    /// </summary>
    /// <param name="path">
    /// The path to the repository's working directory or Git directory.
    /// </param>
    /// <returns>
    /// A <see cref="Repository"/> instance representing the opened repository.
    /// </returns>
    /// <exception cref="GitException">
    /// The repository could not be opened.
    /// </exception>
    public static Repository Open(InteropString path)
    {
        GitRuntime.Initialize();

        GitRepository* repository = null;

        int code = Native.GitRepositoryOpen(&repository, path);
        GitException.ThrowIfError(code);

        return new Repository(repository);
    }
}