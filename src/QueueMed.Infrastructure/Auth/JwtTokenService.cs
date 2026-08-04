using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using QueueMed.Application.Abstractions;
using QueueMed.Application.Options;
using QueueMed.Domain.Entities;

namespace QueueMed.Infrastructure.Auth;

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public string GenerateToken(Atendente atendente)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, atendente.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, atendente.Usuario),
            new Claim(ClaimTypes.Name, atendente.Nome),
            new Claim(ClaimTypes.Role, "Atendente")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_options.ExpiresMinutes);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
