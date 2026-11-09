using RecordLabel.Infrastructure.Security;

namespace RecordLabel.UnitTests.Security;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Hash_DoesNotContainThePlainPassword()
    {
        var hash = _hasher.Hash("secret");

        Assert.DoesNotContain("secret", hash);
    }

    [Fact]
    public void Hash_SamePasswordTwice_GivesDifferentHashes()
    {
        Assert.NotEqual(_hasher.Hash("secret"), _hasher.Hash("secret"));
    }

    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        var hash = _hasher.Hash("secret");

        Assert.True(_hasher.Verify(hash, "secret"));
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        var hash = _hasher.Hash("secret");

        Assert.False(_hasher.Verify(hash, "Secret"));
    }
}
