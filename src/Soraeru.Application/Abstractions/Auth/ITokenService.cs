namespace Soraeru.Application.Abstractions.Auth;

public interface ITokenService
{
    string CreateAccessToken(Guid userId, string email);
}
