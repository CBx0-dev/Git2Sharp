using System.IO.Compression;
using System.Reflection;

namespace Git2Sharp.Tests;

internal static class FixtureExtractor
{
    private static readonly Assembly Assembly = typeof(FixtureExtractor).Assembly;

    public static void ExtractStandardRepository(string targetDirectory)
    {
        ExtractResource("Git2Sharp.Tests.Fixtures.StandardRepo.zip", targetDirectory);
    }

    public static void ExtractEmptyRepository(string targetDirectory)
    {
        ExtractResource("Git2Sharp.Tests.Fixtures.EmptyRepo.zip", targetDirectory);
    }

    private static void ExtractResource(string resourceName, string targetDirectory)
    {
        using Stream? stream = Assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new InvalidOperationException($"Embedded resource '{resourceName}' was not found.");
        }

        Directory.CreateDirectory(targetDirectory);
        using ZipArchive archive = new(stream, ZipArchiveMode.Read);
        archive.ExtractToDirectory(targetDirectory, overwriteFiles: true);
    }
}
