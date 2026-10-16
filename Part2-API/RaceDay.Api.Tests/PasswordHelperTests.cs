using RaceDay.Api.Helpers;

namespace RaceDay.Api.Tests;

public class PasswordHelperTests
{
    [Fact]
    public void Hash_DoesNotContainOriginalPassword()
    {
        var hash = PasswordHelper.Hash("Password1!");

        Assert.NotEqual("Password1!", hash);
        Assert.DoesNotContain("Password1!", hash);
    }

    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        var hash = PasswordHelper.Hash("Password1!");

        Assert.True(PasswordHelper.Verify("Password1!", hash));
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        var hash = PasswordHelper.Hash("Password1!");

        Assert.False(PasswordHelper.Verify("WrongPassword", hash));
    }

    [Fact]
    public void Hash_SamePasswordTwice_GivesDifferentHashes()
    {
        // each hash uses a random salt
        Assert.NotEqual(PasswordHelper.Hash("Password1!"), PasswordHelper.Hash("Password1!"));
    }
}
