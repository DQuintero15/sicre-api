using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Sicre.Api.Config;
using Sicre.Api.Domain.Entities;
using Sicre.Api.Shared;

namespace Sicre.Api.Features.Auth.Services;

public interface ITokenService
{
    string GenerateAccessToken(User user, IList<string> roles);
    string GenerateTemporaryToken(User user);
    (string token, DateTime expires) GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
    ClaimsPrincipal? ValidateTokenIgnoreExpiration(string token);
}

public class TokenService(IOptions<AppSettings> options) : ITokenService
{
    private readonly JwtSettings _jwt = options.Value.Jwt;

    public string GenerateAccessToken(User user, IList<string> roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(Constants.ClaimNames.TokenType, Constants.TokenTypes.AccessToken),
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        return GenerateToken([.. claims], _jwt.AccessTokenExpirationMinutes);
    }

    public string GenerateTemporaryToken(User user)
    {
        Claim[] claims =
        [
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(Constants.ClaimNames.TokenType, Constants.TokenTypes.Temporary),
        ];

        return GenerateToken(claims, _jwt.TemporaryTokenExpirationMinutes);
    }

    public (string token, DateTime expires) GenerateRefreshToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return (
            Convert.ToBase64String(bytes),
            DateTime.UtcNow.AddDays(_jwt.RefreshTokenExpirationDays)
        );
    }

    public ClaimsPrincipal? ValidateToken(string token) => Validate(token, validateLifetime: true);

    public ClaimsPrincipal? ValidateTokenIgnoreExpiration(string token) =>
        Validate(token, validateLifetime: false);

    private ClaimsPrincipal? Validate(string token, bool validateLifetime)
    {
        try
        {
            return new JwtSecurityTokenHandler().ValidateToken(
                token,
                BuildParams(validateLifetime),
                out _
            );
        }
        catch
        {
            return null;
        }
    }

    private TokenValidationParameters BuildParams(bool validateLifetime) =>
        new()
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SecretKey)),
            ValidateIssuer = true,
            ValidIssuer = _jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = _jwt.Audience,
            ValidateLifetime = validateLifetime,
            ClockSkew = TimeSpan.Zero,
        };

    private string GenerateToken(Claim[] claims, int expirationMinutes)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
            Issuer = _jwt.Issuer,
            Audience = _jwt.Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SecretKey)),
                SecurityAlgorithms.HmacSha256Signature
            ),
        };

        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(handler.CreateToken(descriptor));
    }
}
