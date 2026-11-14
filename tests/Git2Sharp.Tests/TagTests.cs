using Git2Sharp.Common;

namespace Git2Sharp.Tests;

public sealed class TagTests
{
    [Fact]
    public void All_ShouldReturnTags()
    {
        using Repository repository = Repository.Open(TestHelper.Repository);
        using NativeCollection<Tag> collection = repository.Tags.All;

        Assert.NotEmpty(collection);
    }
}