using System.Collections.Generic;
using Git2Sharp.Bindings;
using Git2Sharp.Common;

namespace Git2Sharp.Handlers;

public sealed unsafe class TagHandler
{
    private readonly Repository _repository;

    public TagHandler(Repository repository)
    {
        _repository = repository;
    }

    public NativeCollection<Tag> All => CollectTags();

    private NativeCollection<Tag> CollectTags()
    {
        using NativeHandleLease<GitRepository> repository = _repository.Handle.Acquire();

        GitStrarray arr = default;
        int result = Native.GitTagList(&arr, repository.Object);
        GitException.ThrowIfError(result);

        List<Tag> tags = new List<Tag>((int)arr.Count);

        for (nuint i = 0; i < arr.Count; i++)
        {
            InteropString name = arr.Strings[i];
            tags.Add(new Tag(name));
        }

        return new NativeCollection<Tag>(tags);
    }
}