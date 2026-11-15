namespace Git2Sharp.Tests;

public sealed class DisposableRepository : IDisposable
{
    private int _disposed;

    public string Path { get; }

    private DisposableRepository(string path)
    {
        Path = path;
    }

    public static DisposableRepository CreateEmpty()
    {
        string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Git2Sharp_Test_" + Guid.NewGuid().ToString("N"));
        FixtureExtractor.ExtractEmptyRepository(path);
        return new DisposableRepository(path);
    }

    public static DisposableRepository CreateInitialized()
    {
        string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Git2Sharp_Test_" + Guid.NewGuid().ToString("N"));
        FixtureExtractor.ExtractStandardRepository(path);
        return new DisposableRepository(path);
    }

    public Repository Open()
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, this);
        return Repository.Open(Path);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        DeleteDirectory(Path);
    }

    internal static void DeleteDirectory(string targetDir)
    {
        if (!Directory.Exists(targetDir))
        {
            return;
        }

        for (int attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                NormalizeAttributes(targetDir);
                Directory.Delete(targetDir, recursive: true);
                return;
            }
            catch (Exception) when (attempt < 2)
            {
                Thread.Sleep(20);
            }
            catch
            {
                // Suppress final exception to prevent teardown failure from masking test outcomes
            }
        }
    }

    private static void NormalizeAttributes(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            return;
        }

        foreach (string file in Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories))
        {
            try
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }
            catch
            {
                // Best-effort attribute reset
            }
        }

        foreach (string subDir in Directory.EnumerateDirectories(directoryPath, "*", SearchOption.AllDirectories))
        {
            try
            {
                File.SetAttributes(subDir, FileAttributes.Normal);
            }
            catch
            {
                // Best-effort attribute reset
            }
        }
    }
}
