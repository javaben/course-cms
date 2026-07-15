using CMS.API.Infrastructure;

namespace CMS.API.Tests;

public class PasswordPolicyTests
{
    [Theory]
    [InlineData("Abcdefg1")]   // upper + lower + digit (3 classes), length 8
    [InlineData("NewPass#1")]  // 4 classes
    [InlineData("aB3$aB3$")]   // 4 classes
    [InlineData("PASSword!")]  // upper + lower + symbol
    public void Accepts_len8_plus_at_least_3_classes(string password)
        => Assert.True(PasswordPolicy.IsComplexEnough(password));

    [Theory]
    [InlineData("")]           // empty
    [InlineData(null)]         // null
    [InlineData("Ab1#")]       // < 8
    [InlineData("Abc1#67")]    // 7 chars (< 8) even with 4 classes
    [InlineData("abcdefgh")]   // 1 class
    [InlineData("abcdefg1")]   // 2 classes (lower + digit)
    [InlineData("ABCDEFG1")]   // 2 classes (upper + digit)
    [InlineData("Abcdefgh")]   // 2 classes (upper + lower)
    public void Rejects_short_or_fewer_than_3_classes(string? password)
        => Assert.False(PasswordPolicy.IsComplexEnough(password));
}
