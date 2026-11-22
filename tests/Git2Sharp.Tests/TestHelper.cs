namespace Git2Sharp.Tests;

public static class TestHelper
{
    public static DisposableRepository CreateEmptyRepository() => DisposableRepository.CreateEmpty();

    public static DisposableRepository CreateInitializedRepository() => DisposableRepository.CreateInitialized();
}