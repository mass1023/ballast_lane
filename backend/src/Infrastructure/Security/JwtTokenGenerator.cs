using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using RecordLabel.Application.Abstractions;
using RecordLabel.Domain.Users;

namespace RecordLabel.Infrastructure.Security;

public class JwtTokenGenerator : ITokenGenerator
{
    private readonly JwtOptions _jwt;
    private readonly TimeProvider _clock;

    public JwtTokenGenerator(IOptions<JwtOptions> options, TimeProvider timeProvider)
    {
        _clock = timeProvider;
        _jwt = options.Value;
    }

    public AccessToken Generate(User user)
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        var expiresAt = now.AddMinutes(_jwt.ExpirationMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username)
            ]),
            Issuer = _jwt.Issuer,
            Audience = _jwt.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };

        return new AccessToken(new JsonWebTokenHandler().CreateToken(descriptor), expiresAt);
    }
}
