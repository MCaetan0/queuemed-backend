using Microsoft.AspNetCore.SignalR;
using QueueMed.Application.Abstractions;
using QueueMed.Application.DTOs;
using QueueMed.Domain.Enums;

namespace QueueMed.Api.Hubs;

public class FilaHub : Hub
{
    public static string TicketGroup(Guid ticketId) => $"ticket:{ticketId}";
    public static string FilaGroup(Especialidade especialidade) => $"fila:{especialidade}";

    public Task JoinTicket(Guid ticketId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, TicketGroup(ticketId));

    public Task LeaveTicket(Guid ticketId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, TicketGroup(ticketId));

    public Task JoinFila(Especialidade especialidade) =>
        Groups.AddToGroupAsync(Context.ConnectionId, FilaGroup(especialidade));

    public Task LeaveFila(Especialidade especialidade) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, FilaGroup(especialidade));
}

public class SignalRQueueNotifier : IQueueNotifier
{
    private readonly IHubContext<FilaHub> _hub;

    public SignalRQueueNotifier(IHubContext<FilaHub> hub)
    {
        _hub = hub;
    }

    public Task NotifyTicketUpdatedAsync(TicketDto ticket, CancellationToken cancellationToken = default) =>
        _hub.Clients.Group(FilaHub.TicketGroup(ticket.Id))
            .SendAsync("TicketUpdated", ticket, cancellationToken);

    public async Task NotifyFilaUpdatedAsync(
        Especialidade especialidade,
        IReadOnlyList<TicketDto> fila,
        CancellationToken cancellationToken = default)
    {
        await _hub.Clients.Group(FilaHub.FilaGroup(especialidade))
            .SendAsync("FilaUpdated", new FilaResponse(especialidade, fila), cancellationToken);
    }

    public async Task NotifyTicketChamadoAsync(TicketDto ticket, CancellationToken cancellationToken = default)
    {
        await _hub.Clients.Group(FilaHub.FilaGroup(ticket.Especialidade))
            .SendAsync("TicketChamado", ticket, cancellationToken);

        await _hub.Clients.Group(FilaHub.TicketGroup(ticket.Id))
            .SendAsync("TicketUpdated", ticket, cancellationToken);
    }
}
