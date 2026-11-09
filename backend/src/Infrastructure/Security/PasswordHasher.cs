using Microsoft.AspNetCore.Identity;
using RecordLabel.Application.Abstractions;
using RecordLabel.Domain.Users;

namespace RecordLabel.Infrastructure.Security;

public class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _inner = new();

    public string Hash(string password) => _inner.HashPassword(null!, password);

    public bool Verify(string passwordHash, string password) =>
        _inner.VerifyHashedPassword(null!, passwordHash, password) != PasswordVerificationResult.Failed;
}
