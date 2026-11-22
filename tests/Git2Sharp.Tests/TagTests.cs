using System.Linq;
using Git2Sharp.Bindings;
using Git2Sharp.Common;

namespace Git2Sharp.Tests;

public sealed class TagTests
{
    [Fact]
    public void All_ShouldReturnTags()
    {
        using DisposableRepository git = TestHelper.CreateInitializedRepository();

        using Repository repository = Repository.Open(git.Path);
        using NativeCollection<Tag> collection = repository.Tags.All;

        Assert.NotEmpty(collection);
        Assert.Contains(collection, t => t.Name == "v1.0.0");
    }

    [Fact]
    public void Name_ShouldReturnTagName()
    {
        using DisposableRepository git = TestHelper.CreateInitializedRepository();

        using Repository repository = Repository.Open(git.Path);
        using Tag tag = repository.Tags.GetTag("v1.0.0");

        Assert.Equal("v1.0.0", tag.Name);
    }

    [Fact]
    public void Name_ShouldCacheName()
    {
        using DisposableRepository git = TestHelper.CreateInitializedRepository();

        using Repository repository = Repository.Open(git.Path);
        using Tag tag = repository.Tags.GetTag("v1.0.0");

        string name1 = tag.Name;
        string name2 = tag.Name;

        Assert.True(ReferenceEquals(name1, name2));
    }

    [Fact]
    public void Repository_ShouldReturnRepository()
    {
        using DisposableRepository git = TestHelper.CreateInitializedRepository();

        using Repository repository = Repository.Open(git.Path);
        using Tag tag = repository.Tags.GetTag("v1.0.0");

        Assert.Same(repository, tag.Repository);
    }

    [Fact]
    public void Commit_ShouldReturnTargetCommit()
    {
        using DisposableRepository git = TestHelper.CreateInitializedRepository();

        using Repository repository = Repository.Open(git.Path);
        using Tag tag = repository.Tags.GetTag("v1.0.0");

        using Commit commit = tag.Commit;

        Assert.NotNull(commit);
        Assert.Same(repository, commit.Repository);
        Assert.Equal("bc9460c37820855fb9f26850331550d280d39cfa", commit.Id.Sha, ignoreCase: true);
        Assert.Equal("Initial commit", commit.Summary);
    }

    [Fact]
    public void GetTag_ShouldReturnTag_WhenExists()
    {
        using DisposableRepository git = TestHelper.CreateInitializedRepository();

        using Repository repository = Repository.Open(git.Path);
        using Tag tag = repository.Tags.GetTag("v1.0.0");

        Assert.NotNull(tag);
        Assert.Equal("v1.0.0", tag.Name);
    }

    [Fact]
    public void GetTag_WithFullRefName_ShouldReturnTag()
    {
        using DisposableRepository git = TestHelper.CreateInitializedRepository();

        using Repository repository = Repository.Open(git.Path);
        using Tag tag = repository.Tags.GetTag("refs/tags/v1.0.0");

        Assert.NotNull(tag);
        Assert.Equal("v1.0.0", tag.Name);
    }

    [Fact]
    public void GetTag_ShouldThrow_WhenTagDoesNotExist()
    {
        using DisposableRepository git = TestHelper.CreateInitializedRepository();

        using Repository repository = Repository.Open(git.Path);

        GitException exception = Assert.Throws<GitException>(() => repository.Tags.GetTag("non-existent-tag"));
        Assert.Equal(GitErrorCode.GIT_ENOTFOUND, exception.Code);
    }

    [Fact]
    public void GetTagOrDefault_ShouldReturnTag_WhenExists()
    {
        using DisposableRepository git = TestHelper.CreateInitializedRepository();

        using Repository repository = Repository.Open(git.Path);
        using Tag? tag = repository.Tags.GetTagOrDefault("v1.0.0");

        Assert.NotNull(tag);
        Assert.Equal("v1.0.0", tag.Name);
    }

    [Fact]
    public void GetTagOrDefault_WithFullRefName_ShouldReturnTag()
    {
        using DisposableRepository git = TestHelper.CreateInitializedRepository();

        using Repository repository = Repository.Open(git.Path);
        using Tag? tag = repository.Tags.GetTagOrDefault("refs/tags/v1.0.0");

        Assert.NotNull(tag);
        Assert.Equal("v1.0.0", tag.Name);
    }

    [Fact]
    public void GetTagOrDefault_ShouldReturnNull_WhenTagDoesNotExist()
    {
        using DisposableRepository git = TestHelper.CreateInitializedRepository();

        using Repository repository = Repository.Open(git.Path);
        using Tag? tag = repository.Tags.GetTagOrDefault("non-existent-tag");

        Assert.Null(tag);
    }

    [Fact]
    public void NewlyCreatedTag_ShouldBeResolvable()
    {
        using DisposableRepository git = TestHelper.CreateInitializedRepository();
        git.ExecuteGit("tag -a v2.0.0 -m \"Release 2.0.0\"");

        using Repository repository = Repository.Open(git.Path);
        using Tag tag = repository.Tags.GetTag("v2.0.0");

        Assert.NotNull(tag);
        Assert.Equal("v2.0.0", tag.Name);

        using Commit commit = tag.Commit;
        Assert.NotNull(commit);
        Assert.Equal("Initial commit", commit.Summary);
    }
}