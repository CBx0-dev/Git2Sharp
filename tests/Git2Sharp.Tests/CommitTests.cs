using Git2Sharp.Bindings;
using Git2Sharp.Common;

namespace Git2Sharp.Tests;

public sealed class CommitTests
{
    [Fact]
    public void GetCommit_ByString_ShouldReturnCommit_WhenExists()
    {
        using DisposableRepository git = TestHelper.CreateInitializedRepository();

        using Repository repository = Repository.Open(git.Path);
        using Commit commit = repository.Commits.GetCommit("bc9460c37820855fb9f26850331550d280d39cfa");

        Assert.NotNull(commit);
        Assert.Equal("bc9460c37820855fb9f26850331550d280d39cfa", commit.Id.Sha, ignoreCase: true);
        Assert.Equal("Initial commit", commit.Summary);
    }

    [Fact]
    public void GetCommit_ByString_ShouldThrow_WhenCommitNotFound()
    {
        using DisposableRepository git = TestHelper.CreateInitializedRepository();

        using Repository repository = Repository.Open(git.Path);

        GitException exception = Assert.Throws<GitException>(() =>
            repository.Commits.GetCommit("0000000000000000000000000000000000000000"));
        Assert.Equal(GitErrorCode.GIT_ENOTFOUND, exception.Code);
    }

    [Fact]
    public void GetCommit_ByString_ShouldThrow_WhenHashInvalid()
    {
        using DisposableRepository git = TestHelper.CreateInitializedRepository();

        using Repository repository = Repository.Open(git.Path);

        Assert.ThrowsAny<ArgumentException>(() => repository.Commits.GetCommit("invalid-hash"));
    }

    [Fact]
    public void GetCommit_ByObjectId_ShouldReturnCommit_WhenExists()
    {
        using DisposableRepository git = TestHelper.CreateInitializedRepository();

        using Repository repository = Repository.Open(git.Path);
        ObjectId id = ObjectId.Parse("bc9460c37820855fb9f26850331550d280d39cfa");

        using Commit commit = repository.Commits.GetCommit(id);

        Assert.NotNull(commit);
        Assert.Equal(id, commit.Id);
        Assert.Equal("Initial commit", commit.Summary);
    }

    [Fact]
    public void GetCommit_ByObjectId_ShouldThrow_WhenCommitNotFound()
    {
        using DisposableRepository git = TestHelper.CreateInitializedRepository();

        using Repository repository = Repository.Open(git.Path);
        ObjectId nonExistentId = ObjectId.Parse("0000000000000000000000000000000000000000");

        GitException exception = Assert.Throws<GitException>(() => repository.Commits.GetCommit(nonExistentId));
        Assert.Equal(GitErrorCode.GIT_ENOTFOUND, exception.Code);
    }

    [Fact]
    public void GetCommitOrDefault_ByString_ShouldReturnCommit_WhenExists()
    {
        using DisposableRepository git = TestHelper.CreateInitializedRepository();

        using Repository repository = Repository.Open(git.Path);
        using Commit? commit = repository.Commits.GetCommitOrDefault("bc9460c37820855fb9f26850331550d280d39cfa");

        Assert.NotNull(commit);
        Assert.Equal("bc9460c37820855fb9f26850331550d280d39cfa", commit.Id.Sha, ignoreCase: true);
        Assert.Equal("Initial commit", commit.Summary);
    }

    [Fact]
    public void GetCommitOrDefault_ByString_ShouldReturnNull_WhenCommitNotFound()
    {
        using DisposableRepository git = TestHelper.CreateInitializedRepository();

        using Repository repository = Repository.Open(git.Path);
        using Commit? commit = repository.Commits.GetCommitOrDefault("0000000000000000000000000000000000000000");

        Assert.Null(commit);
    }

    [Fact]
    public void GetCommitOrDefault_ByString_ShouldThrow_WhenHashInvalid()
    {
        using DisposableRepository git = TestHelper.CreateInitializedRepository();

        using Repository repository = Repository.Open(git.Path);

        Assert.ThrowsAny<ArgumentException>(() => repository.Commits.GetCommitOrDefault("invalid-hash"));
    }

    [Fact]
    public void GetCommitOrDefault_ByObjectId_ShouldReturnCommit_WhenExists()
    {
        using DisposableRepository git = TestHelper.CreateInitializedRepository();

        using Repository repository = Repository.Open(git.Path);
        ObjectId id = ObjectId.Parse("bc9460c37820855fb9f26850331550d280d39cfa");

        using Commit? commit = repository.Commits.GetCommitOrDefault(id);

        Assert.NotNull(commit);
        Assert.Equal(id, commit.Id);
        Assert.Equal("Initial commit", commit.Summary);
    }

    [Fact]
    public void GetCommitOrDefault_ByObjectId_ShouldReturnNull_WhenCommitNotFound()
    {
        using DisposableRepository git = TestHelper.CreateInitializedRepository();

        using Repository repository = Repository.Open(git.Path);
        ObjectId nonExistentId = ObjectId.Parse("0000000000000000000000000000000000000000");

        using Commit? commit = repository.Commits.GetCommitOrDefault(nonExistentId);

        Assert.Null(commit);
    }

    [Fact]
    public void Summary_ShouldReturnCommitSummary()
    {
        using DisposableRepository git = TestHelper.CreateInitializedRepository();

        using Repository repository = Repository.Open(git.Path);
        using Commit commit = repository.Commits.GetCommit("bc9460c37820855fb9f26850331550d280d39cfa");

        Assert.Equal("Initial commit", commit.Summary);
    }

    [Fact]
    public void Body_ShouldReturnNull_WhenCommitHasNoBody()
    {
        using DisposableRepository git = TestHelper.CreateInitializedRepository();

        using Repository repository = Repository.Open(git.Path);
        using Commit commit = repository.Commits.GetCommit("bc9460c37820855fb9f26850331550d280d39cfa");

        Assert.Null(commit.Body);
    }

    [Fact]
    public void Body_ShouldReturnBody_WhenCommitHasBody()
    {
        using DisposableRepository git = TestHelper.CreateInitializedRepository();
        git.ExecuteGit("commit --allow-empty -m \"Header summary\n\nThis is the first line of body.\nThis is the second line of body.\"");

        using Repository repository = Repository.Open(git.Path);
        using Branch branch = repository.Branches.GetBranch("master", GitBranchT.GIT_BRANCH_LOCAL);
        using Commit commit = branch.Head;

        Assert.Equal("Header summary", commit.Summary);
        Assert.Equal("This is the first line of body.\nThis is the second line of body.", commit.Body);
    }

    [Fact]
    public void Author_ShouldReturnAuthorDetails()
    {
        using DisposableRepository git = TestHelper.CreateInitializedRepository();

        using Repository repository = Repository.Open(git.Path);
        using Commit commit = repository.Commits.GetCommit("bc9460c37820855fb9f26850331550d280d39cfa");

        Assert.NotNull(commit.Author);
        Assert.Equal("Git2Sharp Test", commit.Author.Name);
        Assert.Equal("test@git2sharp.local", commit.Author.Email);
        Assert.Equal(new DateTime(2026, 11, 14, 18, 0, 28), commit.Author.When);
    }

    [Fact]
    public void Id_ShouldReturnObjectId_AndCacheInstance()
    {
        using DisposableRepository git = TestHelper.CreateInitializedRepository();

        using Repository repository = Repository.Open(git.Path);
        using Commit commit = repository.Commits.GetCommit("bc9460c37820855fb9f26850331550d280d39cfa");

        ObjectId id = commit.Id;
        ObjectId cachedId = commit.Id;

        Assert.Equal("bc9460c37820855fb9f26850331550d280d39cfa", id.Sha, ignoreCase: true);
        Assert.Same(id, cachedId);
    }

    [Fact]
    public void Repository_ShouldReturnOwnerRepository()
    {
        using DisposableRepository git = TestHelper.CreateInitializedRepository();

        using Repository repository = Repository.Open(git.Path);
        using Commit commit = repository.Commits.GetCommit("bc9460c37820855fb9f26850331550d280d39cfa");

        Assert.Same(repository, commit.Repository);
    }

    [Fact]
    public void Parent_ShouldThrow_WhenRootCommit()
    {
        using DisposableRepository git = TestHelper.CreateInitializedRepository();

        using Repository repository = Repository.Open(git.Path);
        using Commit commit = repository.Commits.GetCommit("bc9460c37820855fb9f26850331550d280d39cfa");

        GitException exception = Assert.Throws<GitException>(() => commit.Parent);
        Assert.Equal(GitErrorCode.GIT_ENOTFOUND, exception.Code);
    }

    [Fact]
    public void Parent_ShouldReturnFirstParent_WhenHasParent()
    {
        using DisposableRepository git = TestHelper.CreateInitializedRepository();
        git.ExecuteGit("commit --allow-empty -m \"Second commit\"");

        using Repository repository = Repository.Open(git.Path);
        using Branch branch = repository.Branches.GetBranch("master", GitBranchT.GIT_BRANCH_LOCAL);
        using Commit secondCommit = branch.Head;
        using Commit parentCommit = secondCommit.Parent;

        Assert.NotNull(parentCommit);
        Assert.Equal("bc9460c37820855fb9f26850331550d280d39cfa", parentCommit.Id.Sha, ignoreCase: true);
        Assert.Equal("Initial commit", parentCommit.Summary);
    }

    [Fact]
    public void Parents_ShouldReturnEmpty_WhenRootCommit()
    {
        using DisposableRepository git = TestHelper.CreateInitializedRepository();

        using Repository repository = Repository.Open(git.Path);
        using Commit commit = repository.Commits.GetCommit("bc9460c37820855fb9f26850331550d280d39cfa");

        using NativeCollection<Commit> parents = commit.Parents;
        Assert.Empty(parents);
    }

    [Fact]
    public void Parents_ShouldReturnSingleParent_WhenSingleParent()
    {
        using DisposableRepository git = TestHelper.CreateInitializedRepository();
        git.ExecuteGit("commit --allow-empty -m \"Second commit\"");

        using Repository repository = Repository.Open(git.Path);
        using Branch branch = repository.Branches.GetBranch("master", GitBranchT.GIT_BRANCH_LOCAL);
        using Commit secondCommit = branch.Head;

        using NativeCollection<Commit> parents = secondCommit.Parents;
        Assert.Single(parents);
        Assert.Equal("bc9460c37820855fb9f26850331550d280d39cfa", parents[0].Id.Sha, ignoreCase: true);
    }

    [Fact]
    public void Parents_ShouldReturnMultipleParents_WhenMergeCommit()
    {
        using DisposableRepository git = TestHelper.CreateInitializedRepository();
        git.ExecuteGit("checkout -b feature");
        git.ExecuteGit("commit --allow-empty -m \"Feature commit\"");
        git.ExecuteGit("checkout master");
        git.ExecuteGit("commit --allow-empty -m \"Master commit\"");
        git.ExecuteGit("merge feature --no-ff -m \"Merge commit\"");

        using Repository repository = Repository.Open(git.Path);
        using Branch masterBranch = repository.Branches.GetBranch("master", GitBranchT.GIT_BRANCH_LOCAL);
        using Commit mergeCommit = masterBranch.Head;

        using NativeCollection<Commit> parents = mergeCommit.Parents;
        Assert.Equal(2, parents.Count);

        using Commit parent0 = mergeCommit.Parent;
        Assert.Equal(parents[0].Id, parent0.Id);
        Assert.Equal("Master commit", parents[0].Summary);
        Assert.Equal("Feature commit", parents[1].Summary);
    }

    [Fact]
    public void Dispose_ShouldFreeHandle()
    {
        using DisposableRepository git = TestHelper.CreateInitializedRepository();

        using Repository repository = Repository.Open(git.Path);
        Commit commit = repository.Commits.GetCommit("bc9460c37820855fb9f26850331550d280d39cfa");
        commit.Dispose();

        Assert.Throws<ObjectDisposedException>(() => commit.Summary);
    }
}