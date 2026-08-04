using QueueMed.Domain.Entities;

namespace QueueMed.Application.Abstractions;

public interface IJwtTokenService
{
    string GenerateToken(Atendente atendente);
}
