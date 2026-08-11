using Microsoft.AspNetCore.Identity;
using Soraeru.Application.Abstractions.Auth;

namespace Soraeru.Infrastructure.Auth;

public sealed class AspNetIdentityPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<object> _hasher = new();

    public string HashPassword(string password) =>
        _hasher.HashPassword(new object(), password);

    public bool VerifyHashedPassword(string hashedPassword, string providedPassword)
    {
        var result = _hasher.VerifyHashedPassword(new object(), hashedPassword, providedPassword);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
