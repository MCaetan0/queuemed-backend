using Microsoft.Extensions.Options;
using QueueMed.Application.Abstractions;
using QueueMed.Application.DTOs;
using QueueMed.Application.Exceptions;
using QueueMed.Application.Options;
using QueueMed.Domain.Entities;

namespace QueueMed.Application.Services;

public class AuthService
{
    private static readonly Guid SeedAtendenteId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly SeedOptions _seed;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(IOptions<SeedOptions> seedOptions, IJwtTokenService jwtTokenService)
    {
        _seed = seedOptions.Value;
        _jwtTokenService = jwtTokenService;
    }

    public Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!string.Equals(request.Usuario, _seed.AtendenteUsuario, StringComparison.Ordinal)
            || !string.Equals(request.Senha, _seed.AtendenteSenha, StringComparison.Ordinal))
        {
            throw new UnauthorizedAppException("Usuário ou senha inválidos.");
        }

        var atendente = new Atendente
        {
            Id = SeedAtendenteId,
            Usuario = _seed.AtendenteUsuario,
            Nome = _seed.AtendenteNome,
            Ativo = true
        };

        var token = _jwtTokenService.GenerateToken(atendente);
        return Task.FromResult(new LoginResponse(token, atendente.Nome, atendente.Id));
    }
}
