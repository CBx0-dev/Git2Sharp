using Git2Sharp.Bindings;

namespace Git2Sharp.Tests;

public sealed class RepositoryTests
{
    [Fact]
    public void Open_ShouldWorkForExistingRepository()
    {
        using Repository repository = Repository.Open(TestHelper.Repository);
    }

    [Fact]
    public void Open_ShouldThrowForNonExistingRepository()
    {
        GitException exception = Assert.Throws<GitException>(() => Repository.Open("non-existing-repository"));
        Assert.Equal(GitErrorCode.GIT_ENOTFOUND, exception.Code);
    }
}