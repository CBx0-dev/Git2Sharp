using System.Diagnostics;

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

    public void ExecuteGit(string arguments)
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, this);

        using Process process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = Path,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        process.Start();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            string error = process.StandardError.ReadToEnd();
            throw new InvalidOperationException($"Git command failed with exit code {process.ExitCode}: {error}");
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        DeleteDirectory(Path);
    }

    private static void DeleteDirectory(string targetDir)
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