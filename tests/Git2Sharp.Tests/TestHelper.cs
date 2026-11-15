namespace Git2Sharp.Tests;

public static class TestHelper
{
    private static readonly Lazy<string> CachedRepository = new Lazy<string>(InitializeCachedRepository, LazyThreadSafetyMode.ExecutionAndPublication);

    public static string Repository => CachedRepository.Value;

    public static DisposableRepository CreateEmptyRepository() => DisposableRepository.CreateEmpty();

    public static DisposableRepository CreateInitializedRepository() => DisposableRepository.CreateInitialized();

    private static string InitializeCachedRepository()
    {
        string path = Path.Combine(Path.GetTempPath(), "Git2Sharp_SharedFixture_" + Guid.NewGuid().ToString("N"));
        FixtureExtractor.ExtractStandardRepository(path);

        AppDomain.CurrentDomain.ProcessExit += (_, _) => DisposableRepository.DeleteDirectory(path);

        return path;
    }
}