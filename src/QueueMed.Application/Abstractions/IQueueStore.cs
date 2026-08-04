using QueueMed.Domain.Entities;
using QueueMed.Domain.Enums;

namespace QueueMed.Application.Abstractions;

public interface IQueueStore
{
    Task<long> IncrementSequenceAsync(DateOnly date, TipoAtendimento tipo, CancellationToken cancellationToken = default);

    Task SaveNewTicketAsync(Ticket ticket, CancellationToken cancellationToken = default);

    Task<Ticket?> GetTicketAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Ticket>> GetWaitingAsync(Especialidade especialidade, CancellationToken cancellationToken = default);

    Task<int> GetConsecutivePreferentialAsync(
        DateOnly date,
        Especialidade especialidade,
        CancellationToken cancellationToken = default);

    Task MarkCalledAsync(
        Ticket ticket,
        DateOnly date,
        int consecutivePreferential,
        CancellationToken cancellationToken = default);

    Task<bool> RemoveTicketAsync(Guid id, CancellationToken cancellationToken = default);
}
