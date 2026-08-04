using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QueueMed.Application.Abstractions;
using QueueMed.Application.DTOs;
using QueueMed.Application.Exceptions;
using QueueMed.Application.Options;
using QueueMed.Domain.Enums;

namespace QueueMed.Application.Services;

public class QueueService
{
    private readonly IQueueStore _store;
    private readonly IClock _clock;
    private readonly IQueueNotifier _notifier;
    private readonly TicketService _ticketService;
    private readonly QueueOptions _queueOptions;
    private readonly ILogger<QueueService> _logger;

    public QueueService(
        IQueueStore store,
        IClock clock,
        IQueueNotifier notifier,
        TicketService ticketService,
        IOptions<QueueOptions> queueOptions,
        ILogger<QueueService> logger)
    {
        _store = store;
        _clock = clock;
        _notifier = notifier;
        _ticketService = ticketService;
        _queueOptions = queueOptions.Value;
        _logger = logger;
    }

    public async Task<FilaResponse> GetFilaAsync(Especialidade especialidade, CancellationToken cancellationToken = default)
    {
        var tickets = await _ticketService.BuildFilaDtosAsync(especialidade, cancellationToken);
        return new FilaResponse(especialidade, tickets);
    }

    public async Task<TicketDto> CallNextAsync(Especialidade especialidade, CancellationToken cancellationToken = default)
    {
        var waiting = await _store.GetWaitingAsync(especialidade, cancellationToken);
        var consecutive = await _store.GetConsecutivePreferentialAsync(
            _clock.Today, especialidade, cancellationToken);

        var next = QueuePrioritySelector.SelectNext(
            waiting,
            consecutive,
            _queueOptions.PreferentialCallsBeforeNormal);

        if (next is null)
        {
            throw new NotFoundException($"Não há pacientes aguardando na fila de {especialidade}.");
        }

        var from = next.Status;
        next.Status = TicketStatus.Chamado;
        next.ChamadoEm = _clock.UtcNow;
        var newConsecutive = QueuePrioritySelector.NextConsecutiveCount(next, consecutive);

        await _store.MarkCalledAsync(next, _clock.Today, newConsecutive, cancellationToken);

        _logger.LogInformation(
            "Ticket called. TicketId={TicketId} CodigoSenha={CodigoSenha} Especialidade={Especialidade} FromStatus={FromStatus} ToStatus={ToStatus} ConsecutivePreferential={Consecutive}",
            next.Id, next.CodigoSenha, especialidade, from, next.Status, newConsecutive);

        var dto = TicketMapper.ToDto(next);
        await _notifier.NotifyTicketChamadoAsync(dto, cancellationToken);
        await _notifier.NotifyTicketUpdatedAsync(dto, cancellationToken);
        await _ticketService.NotifyFilaAsync(especialidade, cancellationToken);
        return dto;
    }
}
