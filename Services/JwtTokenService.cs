using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ADOTTA.Projects.Suite.Api.Configuration;
using ADOTTA.Projects.Suite.Api.Constants;
using ADOTTA.Projects.Suite.Api.DTOs;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ADOTTA.Projects.Suite.Api.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _jwtSettings;

    public JwtTokenService(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
    }

    public JwtTokenResult GenerateToken(UserDto user, string sapSessionId, int sapSessionTimeoutMinutes)
    {
        if (string.IsNullOrWhiteSpace(_jwtSettings.Secret))
        {
            throw new InvalidOperationException("JWT secret is not configured");
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var expires = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenMinutes <= 0
            ? 60
            : _jwtSettings.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Code ?? string.Empty),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.UniqueName, user.UserCode ?? string.Empty),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(ClaimTypes.Role, user.Ruolo ?? string.Empty),
            new(SapClaimTypes.SessionId, sapSessionId),
            new(SapClaimTypes.SessionTimeout, sapSessionTimeoutMinutes.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires,
            signingCredentials: credentials);

        var handler = new JwtSecurityTokenHandler();
        var tokenValue = handler.WriteToken(token);

        return new JwtTokenResult
        {
            Token = tokenValue,
            ExpiresAt = expires,
            ExpiresInSeconds = (int)(expires - DateTime.UtcNow).TotalSeconds
        };
    }
}


