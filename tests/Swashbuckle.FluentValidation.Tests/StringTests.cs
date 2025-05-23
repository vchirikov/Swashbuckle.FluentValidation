using OpenApi.Core;

namespace Swashbuckle.FluentValidation.Tests;

public class StringTests
{
    [Theory]
    [InlineData("BlogId", "blogId", true)]
    [InlineData("blogId", "blogId", true)]
    [InlineData("blogId", "blogId1", false)]
    [InlineData("blogIdentifier", "blog_id", false)]
    [InlineData("blog_id", "BlogId", true)]
    [InlineData("BlogId", "blog-id", true)]
    public void EqualsIgnoreAll(string left, string right, bool shouldBeEqual)
    {
        Assert.Equal(shouldBeEqual, left.EqualsIgnoreAll(right));
    }
}