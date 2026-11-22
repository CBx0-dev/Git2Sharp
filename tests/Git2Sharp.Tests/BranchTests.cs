using Git2Sharp.Bindings;
using Git2Sharp.Common;

namespace Git2Sharp.Tests;

public sealed class BranchTests
{
    [Fact]
    public void All_ShouldReturnBranches()
    {
        using DisposableRepository git = TestHelper.CreateInitializedRepository();

        using Repository repository = Repository.Open(git.Path);
        using NativeCollection<Branch> collection = repository.Branches.All;

        Assert.NotEmpty(collection);
    }

    [Fact]
    public void Local_ShouldReturnBranches()
    {
        using DisposableRepository git = TestHelper.CreateInitializedRepository();

        using Repository repository = Repository.Open(git.Path);
        using NativeCollection<Branch> collection = repository.Branches.Local;

        Assert.NotEmpty(collection);
        Assert.All(collection, b => Assert.True(b.IsLocal));
    }

    [Fact]
    public void Remote_ShouldReturnBranches()
    {
        using DisposableRepository git = TestHelper.CreateInitializedRepository();

        using Repository repository = Repository.Open(git.Path);
        using NativeCollection<Branch> collection = repository.Branches.Remote;

        Assert.NotEmpty(collection);
        Assert.All(collection, b => Assert.True(b.IsRemote));
    }

    [Fact]
    public void Branch_ShouldResolvesHisName()
    {
        using DisposableRepository git = TestHelper.CreateInitializedRepository();

        using Repository repository = Repository.Open(git.Path);
        using NativeCollection<Branch> collection = repository.Branches.All;

        Assert.NotEmpty(collection);
        Assert.Contains(collection, b => b.Name == "master");
    }

    [Fact]
    public void Branch_ShouldCacheName()
    {
        using DisposableRepository git = TestHelper.CreateInitializedRepository();

        using Repository repository = Repository.Open(git.Path);
        using NativeCollection<Branch> collection = repository.Branches.All;

        Assert.NotEmpty(collection);
        Branch? branch = collection.FirstOrDefault(b => b.Name == "master");
        Assert.NotNull(branch);

        string name = branch.Name;
        string cachedName = branch.Name;

        Assert.True(ReferenceEquals(name, cachedName));
    }

    [Fact]
    public void Head_ShouldReturnHeadCommit_ForBranch()
    {
        using DisposableRepository git = TestHelper.CreateInitializedRepository();

        using Repository repository = Repository.Open(git.Path);
        using Branch branch = repository.Branches.GetBranch("master", GitBranchT.GIT_BRANCH_LOCAL);

        using Commit head = branch.Head;

        Assert.NotNull(head);
        Assert.Same(repository, head.Repository);
        Assert.Equal("bc9460c37820855fb9f26850331550d280d39cfa", head.Id.Sha, ignoreCase: true);
        Assert.Equal("Initial commit", head.Summary);
    }

    [Fact]
    public void GetBranch_ShouldReturnBranch_WhenExists()
    {
        using DisposableRepository git = TestHelper.CreateInitializedRepository();

        using Repository repository = Repository.Open(git.Path);
        using Branch branch = repository.Branches.GetBranch("master", GitBranchT.GIT_BRANCH_LOCAL);

        Assert.NotNull(branch);
        Assert.Equal("master", branch.Name);
        Assert.True(branch.IsLocal);
    }

    [Fact]
    public void GetBranch_ShouldThrow_WhenBranchDoesNotExist()
    {
        using DisposableRepository git = TestHelper.CreateInitializedRepository();

        using Repository repository = Repository.Open(git.Path);

        GitException exception = Assert.Throws<GitException>(() =>
            repository.Branches.GetBranch("non-existent-branch", GitBranchT.GIT_BRANCH_LOCAL));
        Assert.Equal(GitErrorCode.GIT_ENOTFOUND, exception.Code);
    }

    [Fact]
    public void GetBranch_ShouldThrow_WhenBranchTypeIsAll()
    {
        using DisposableRepository git = TestHelper.CreateInitializedRepository();

        using Repository repository = Repository.Open(git.Path);

        Assert.Throws<ArgumentException>(() =>
            repository.Branches.GetBranch("master", GitBranchT.GIT_BRANCH_ALL));
    }

    [Fact]
    public void GetBranchOrDefault_ShouldReturnBranch_WhenExists()
    {
        using DisposableRepository git = TestHelper.CreateInitializedRepository();

        using Repository repository = Repository.Open(git.Path);
        using Branch? branch = repository.Branches.GetBranchOrDefault("master", GitBranchT.GIT_BRANCH_LOCAL);

        Assert.NotNull(branch);
        Assert.Equal("master", branch.Name);
        Assert.True(branch.IsLocal);
    }

    [Fact]
    public void GetBranchOrDefault_ShouldReturnNull_WhenBranchDoesNotExist()
    {
        using DisposableRepository git = TestHelper.CreateInitializedRepository();

        using Repository repository = Repository.Open(git.Path);
        using Branch? branch = repository.Branches.GetBranchOrDefault("non-existent-branch", GitBranchT.GIT_BRANCH_LOCAL);

        Assert.Null(branch);
    }

    [Fact]
    public void GetBranchOrDefault_ShouldThrow_WhenBranchTypeIsAll()
    {
        using DisposableRepository git = TestHelper.CreateInitializedRepository();

        using Repository repository = Repository.Open(git.Path);

        Assert.Throws<ArgumentException>(() =>
            repository.Branches.GetBranchOrDefault("master", GitBranchT.GIT_BRANCH_ALL));
    }
}