using System;
using Git2Sharp.Bindings;

namespace Git2Sharp.Handlers;

internal sealed unsafe class BranchIterator : IDisposable
{
    private readonly GitBranchIterator* _iterator;

    internal BranchIterator(GitRepository* repository, GitBranchT type)
    {
        GitBranchIterator* iterator;

        int result = Native.GitBranchIteratorNew(&iterator, repository, type);
        GitException.ThrowIfError(result);

        _iterator = iterator;
    }

    public bool MoveNext(out GitReference* value, out GitBranchT branchType)
    {
        GitReference* ptr = null;
        GitBranchT type = default;

        GitErrorCode result = (GitErrorCode)Native.GitBranchNext(&ptr, &type, _iterator);

        switch (result)
        {
            case GitErrorCode.GIT_OK:
                value = ptr;
                branchType = type;
                return true;
            case GitErrorCode.GIT_ITEROVER:
                value = null;
                branchType = default;
                return false;
            default:
                throw new GitException(result);
        }
    }

    public void Dispose()
    {
        if (_iterator is not null)
        {
            Native.GitBranchIteratorFree(_iterator);
        }
    }
}