using System.Runtime.InteropServices;
using LibGit2.Bindings;

namespace Git2Sharp.Sample;

public static class Program
{
    public static unsafe void Main(string[] args)
    {
        sbyte* repoName = (sbyte*)Marshal.StringToCoTaskMemUTF8(Environment.CurrentDirectory);

        int error = Native.GitLibgit2Init();
        Console.WriteLine($">> Init: {error}");

        GitRepository* repo = null;
        GitReference* head = null;

        error = Native.GitRepositoryOpen(&repo, repoName);
        Console.WriteLine($">> Repo: {error}, repo: {(nint)repo}");

        error = Native.GitRepositoryHead(&head, repo);
        Console.WriteLine($">> Head: {error}, repo: {(nint)head}");

        sbyte* name = Native.GitReferenceShorthand(head);
        Console.WriteLine($">> Branch: {Marshal.PtrToStringUTF8((nint)name)}");

        Native.GitReferenceFree(head);
        Native.GitRepositoryFree(repo);
        Native.GitLibgit2Shutdown();
    }
}