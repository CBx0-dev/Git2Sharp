namespace Git2Sharp.Tests;

public sealed class BranchTests
{
    [Fact]
    public void All_ShouldReturnBranches()
    {
        using Repository repository = Repository.Open(TestHelper.Repository);
        using BranchCollection collection = repository.Branches.All;

        Assert.NotEmpty(collection);
    }

    [Fact]
    public void Local_ShouldReturnBranches()
    {
        using Repository repository = Repository.Open(TestHelper.Repository);
        using BranchCollection collection = repository.Branches.Local;

        Assert.NotEmpty(collection);
        Assert.All(collection, b => Assert.True(b.IsLocal));
    }

    [Fact]
    public void Remote_ShouldReturnBranches()
    {
        using Repository repository = Repository.Open(TestHelper.Repository);
        using BranchCollection collection = repository.Branches.Remote;

        Assert.NotEmpty(collection);
        Assert.All(collection, b => Assert.True(b.IsRemote));
    }

    [Fact]
    public void Branch_ShouldResolvesHisName()
    {
        using Repository repository = Repository.Open(TestHelper.Repository);
        using BranchCollection collection = repository.Branches.All;

        Assert.NotEmpty(collection);
        Assert.Contains(collection, b => b.Name == "master");
    }
}