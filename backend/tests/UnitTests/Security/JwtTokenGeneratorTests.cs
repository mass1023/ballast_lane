using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Moq;
using RecordLabel.Domain.Users;
using RecordLabel.Infrastructure.Security;

namespace RecordLabel.UnitTests.Security;

public class JwtTokenGeneratorTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 9, 12, 0, 0, TimeSpan.Zero);

    private static readonly JwtOptions Options = new()
    {
        Secret = "unit-test-secret-that-is-at-least-32-chars",
        Issuer = "record-label-api",
        Audience = "record-label-frontend",
        ExpirationMinutes = 60
    };

    private readonly User _user = new() { Id = Guid.NewGuid(), Username = "admin" };
    private readonly JwtTokenGenerator _generator;

    public JwtTokenGeneratorTests()
    {
        var timeProvider = new Mock<TimeProvider>();
        timeProvider.Setup(x => x.GetUtcNow()).Returns(DateTimeOffset.UtcNow);
        _generator = new JwtTokenGenerator(Microsoft.Extensions.Options.Options.Create(Options), timeProvider.Object);
    }

    private static TokenValidationParameters ValidationParameters(string secret) => new()
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
        ValidIssuer = Options.Issuer,
        ValidAudience = Options.Audience,
        ClockSkew = TimeSpan.Zero
    };

    [Fact]
    public async Task Generate_TokenIsValidAndCarriesTheUser()
    {
        var token = _generator.Generate(_user);

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token.Token, ValidationParameters(Options.Secret));

        Assert.True(result.IsValid, result.Exception?.Message);
        Assert.Equal(_user.Id.ToString(), result.Claims[JwtRegisteredClaimNames.Sub]);
        Assert.Equal("admin", result.Claims[JwtRegisteredClaimNames.UniqueName]);
    }

    [Fact]
    public async Task Generate_TokenSignedWithAnotherKey_IsRejected()
    {
        var token = _generator.Generate(_user);

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token.Token, ValidationParameters("another-secret-that-is-at-least-32-chars"));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Generate_ExpiresAfterConfiguredMinutes()
    {
        var timeProvider = new Mock<TimeProvider>();
        timeProvider.Setup(x => x.GetUtcNow()).Returns(Now);
        var generator = new JwtTokenGenerator(Microsoft.Extensions.Options.Options.Create(Options), timeProvider.Object);

        var token = generator.Generate(_user);

        var expected = Now.UtcDateTime.AddMinutes(Options.ExpirationMinutes);
        Assert.Equal(expected, token.ExpiresAt);
        Assert.Equal(expected, new JsonWebToken(token.Token).ValidTo);
    }
}
