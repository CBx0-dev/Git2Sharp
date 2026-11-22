using System.Collections.Generic;
using Git2Sharp.Bindings;
using Git2Sharp.Common;

namespace Git2Sharp.Handlers;

/// <summary>
/// Provides access to tags in a repository.
/// </summary>
public sealed unsafe class TagHandler
{
    private readonly Repository _repository;

    /// <summary>
    /// Gets all tags in the repository.
    /// </summary>
    /// <returns>A collection containing all tags in the repository.</returns>
    public NativeCollection<Tag> All => CollectTags();

    internal TagHandler(Repository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Gets a tag by its name.
    /// </summary>
    /// <param name="name">The name of the tag to retrieve.</param>
    /// <returns>The matching tag.</returns>
    /// <exception cref="GitException">
    /// The tag could not be found, its name is malformed, or another error occurred.
    /// </exception>
    public Tag GetTag(string name)
    {
        if (!name.StartsWith("refs/tags/"))
        {
            name = $"refs/tags/{name}";
        }

        return GetTag((InteropString)name);
    }

    internal Tag GetTag(InteropString name)
    {
        using NativeHandleLease<GitRepository> repository = _repository.Handle.Acquire();
        GitTag* tag;

        int error = Native.GitRevparseSingle(
            (GitObject**)&tag,
            repository.Object,
            name
        );
        GitException.ThrowIfError(error);

        return new Tag(_repository, tag);
    }

    /// <summary>
    /// Gets a tag by its name, or <see langword="null"/> if it does not exist.
    /// </summary>
    /// <param name="name">The name of the tag to retrieve.</param>
    /// <returns>The matching tag, or <see langword="null"/> if no matching tag exists.</returns>
    /// <exception cref="GitException">
    /// The tag name is malformed or another error occurred.
    /// </exception>
    public Tag? GetTagOrDefault(string name)
    {
        if (!name.StartsWith("refs/tags/"))
        {
            name = $"refs/tags/{name}";
        }

        return GetTagOrDefault((InteropString)name);
    }

    internal Tag? GetTagOrDefault(InteropString name)
    {
        using NativeHandleLease<GitRepository> repository = _repository.Handle.Acquire();
        GitTag* tag;

        int error = Native.GitRevparseSingle(
            (GitObject**)&tag,
            repository.Object,
            name
        );
        if (error == (int)GitErrorCode.GIT_ENOTFOUND)
        {
            return null;
        }
        GitException.ThrowIfError(error);

        return new Tag(_repository, tag);
    }
    
    private NativeCollection<Tag> CollectTags()
    {
        using NativeHandleLease<GitRepository> repository = _repository.Handle.Acquire();

        GitStrarray arr = default;
        int result = Native.GitTagList(&arr, repository.Object);
        GitException.ThrowIfError(result);

        List<Tag> tags = new List<Tag>((int)arr.Count);
        try
        {
            for (nuint i = 0; i < arr.Count; i++)
            {
                string name = "refs/tags/" + (InteropString)arr.Strings[i];
                tags.Add(GetTag(name));
            }
        }
        catch
        {
            Native.GitStrarrayFree(&arr);
            foreach (Tag tag in tags)
            {
                tag.Dispose();
            }

            throw;
        }

        return new NativeCollection<Tag>(tags);
    }
}