using System.Threading;
using Git2Sharp.Bindings;

namespace Git2Sharp;

internal static class GitRuntime
{
    private static bool _initialized;
    private static readonly Lock _lock = new Lock();

    public static void Initialize()
    {
        lock (_lock)
        {
            if (_initialized)
            {
                return;
            }

            int result = Native.GitLibgit2Init();
            GitException.ThrowIfError(result);
            _initialized = true;
        }
    }
}