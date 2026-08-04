using QueueMed.Application.DTOs;
using QueueMed.Domain.Enums;

namespace QueueMed.Application.Abstractions;

public interface IQueueNotifier
{
    Task NotifyTicketUpdatedAsync(TicketDto ticket, CancellationToken cancellationToken = default);
    Task NotifyFilaUpdatedAsync(Especialidade especialidade, IReadOnlyList<TicketDto> fila, CancellationToken cancellationToken = default);
    Task NotifyTicketChamadoAsync(TicketDto ticket, CancellationToken cancellationToken = default);
}
