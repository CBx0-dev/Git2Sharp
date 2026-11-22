using System;

namespace Git2Sharp.Tests;

public sealed class ObjectIdTests
{
    [Fact]
    public void Constructor_ShouldInitializeFromBytes()
    {
        byte[] bytes = Convert.FromHexString("bc9460c37820855fb9f26850331550d280d39cfa");
        ObjectId id = new ObjectId(bytes);

        Assert.Equal(bytes, id.Bytes);
        Assert.Equal("BC9460C37820855FB9F26850331550D280D39CFA", id.Sha);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenInvalidLength()
    {
        byte[] bytes = new byte[19];
        Assert.Throws<ArgumentOutOfRangeException>(() => new ObjectId(bytes));
    }

    [Fact]
    public void Parse_ShouldCreateObjectId_WhenValidSha()
    {
        ObjectId id = ObjectId.Parse("bc9460c37820855fb9f26850331550d280d39cfa");

        Assert.Equal("BC9460C37820855FB9F26850331550D280D39CFA", id.Sha);
    }

    [Fact]
    public void Parse_ShouldThrow_WhenInvalidSha()
    {
        Assert.ThrowsAny<ArgumentException>(() => ObjectId.Parse("invalid"));
        Assert.ThrowsAny<ArgumentException>(() => ObjectId.Parse("bc9460c37820855fb9f26850331550d280d39c"));
    }

    [Fact]
    public void TryParse_ShouldReturnTrue_WhenValidSha()
    {
        bool result = ObjectId.TryParse(out ObjectId? id, "bc9460c37820855fb9f26850331550d280d39cfa");

        Assert.True(result);
        Assert.NotNull(id);
        Assert.Equal("BC9460C37820855FB9F26850331550D280D39CFA", id.Sha);
    }

    [Fact]
    public void TryParse_ShouldReturnFalse_WhenInvalidSha()
    {
        bool result = ObjectId.TryParse(out ObjectId? id, "invalid-hex");

        Assert.False(result);
        Assert.Null(id);
    }

    [Fact]
    public void Equality_ShouldWorkCorrectly()
    {
        ObjectId id1 = ObjectId.Parse("bc9460c37820855fb9f26850331550d280d39cfa");
        ObjectId id2 = ObjectId.Parse("BC9460C37820855FB9F26850331550D280D39CFA");
        ObjectId id3 = ObjectId.Parse("0000000000000000000000000000000000000000");

        Assert.Equal(id1, id2);
        Assert.NotEqual(id1, id3);
        Assert.True(id1 == id2);
        Assert.False(id1 == id3);
    }
}
