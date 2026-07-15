using CMS.API.Infrastructure;

namespace CMS.API.Tests;

public class AppConfigTests
{
    private const string Json =
        "{\"defaultPassword\":\"Abc123!\",\"symmetricSecurityKey\":\"a-very-long-signing-secret\"}";

    [Fact]
    public void Extracts_the_requested_property()
    {
        Assert.Equal("Abc123!", AppConfig.GetRequiredProperty(Json, "defaultPassword"));
        Assert.Equal("a-very-long-signing-secret", AppConfig.GetRequiredProperty(Json, "symmetricSecurityKey"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Throws_when_the_blob_is_missing(string? json)
        => Assert.Throws<InvalidOperationException>(() => AppConfig.GetRequiredProperty(json, "defaultPassword"));

    [Fact]
    public void Throws_when_the_property_is_absent()
        => Assert.Throws<InvalidOperationException>(() => AppConfig.GetRequiredProperty(Json, "missing"));

    [Fact]
    public void Throws_when_the_property_is_blank()
        => Assert.Throws<InvalidOperationException>(
            () => AppConfig.GetRequiredProperty("{\"defaultPassword\":\"\"}", "defaultPassword"));

    [Fact]
    public void Throws_on_invalid_json()
        => Assert.Throws<InvalidOperationException>(() => AppConfig.GetRequiredProperty("not json", "defaultPassword"));
}
