using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using achiev_hub.Server.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace achiev_hub.Server.Support;

public class JwtTokenService
{
    private readonly JwtSettings _settings;

    public JwtTokenService(IOptions<JwtSettings> options)
    {
        _settings = options.Value;
    }

    public const string GuestRole = "guest";
    public const string TokenKindClaim = "token_kind";
    public const string GuestTokenKind = "guest";

    public string GenerateToken(User user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Role, user.Role),
            new("token_version", user.TokenVersion.ToString())
        };

        if (!string.IsNullOrWhiteSpace(user.SteamId))
        {
            claims.Add(new Claim("steam_id", user.SteamId));
        }

        return CreateToken(claims);
    }

    public string GenerateGuestToken(string steamId)
    {
        var normalizedSteamId = steamId.Trim();
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, "guest"),
            new(ClaimTypes.Role, GuestRole),
            new(TokenKindClaim, GuestTokenKind),
            new("steam_id", normalizedSteamId)
        };

        return CreateToken(claims);
    }

    private string CreateToken(IEnumerable<Claim> claims)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
