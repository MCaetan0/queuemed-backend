using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QueueMed.Application.Abstractions;
using QueueMed.Application.DTOs;
using QueueMed.Application.Exceptions;
using QueueMed.Application.Options;
using QueueMed.Domain.Enums;

namespace QueueMed.Application.Services;

public class TicketService
{
    private readonly IQueueStore _store;
    private readonly IClock _clock;
    private readonly IQueueNotifier _notifier;
    private readonly QueueOptions _queueOptions;
    private readonly ILogger<TicketService> _logger;

    public TicketService(
        IQueueStore store,
        IClock clock,
        IQueueNotifier notifier,
        IOptions<QueueOptions> queueOptions,
        ILogger<TicketService> logger)
    {
        _store = store;
        _clock = clock;
        _notifier = notifier;
        _queueOptions = queueOptions.Value;
        _logger = logger;
    }

    public async Task<TicketDto> CreateAsync(CreateTicketRequest request, CancellationToken cancellationToken = default)
    {
        var numero = await _store.IncrementSequenceAsync(_clock.Today, request.TipoAtendimento, cancellationToken);
        var prefix = request.TipoAtendimento == TipoAtendimento.Preferencial ? "P" : "N";
        var codigo = $"{prefix}{numero:D3}";

        var ticket = new Domain.Entities.Ticket
        {
            Id = Guid.NewGuid(),
            CodigoSenha = codigo,
            TipoAtendimento = request.TipoAtendimento,
            Especialidade = request.Especialidade,
            Status = TicketStatus.Aguardando,
            CriadoEm = _clock.UtcNow
        };

        await _store.SaveNewTicketAsync(ticket, cancellationToken);

        _logger.LogInformation(
            "Ticket created. TicketId={TicketId} CodigoSenha={CodigoSenha} Tipo={Tipo} Especialidade={Especialidade} Status={Status}",
            ticket.Id, ticket.CodigoSenha, ticket.TipoAtendimento, ticket.Especialidade, ticket.Status);

        var position = await CalculatePositionAsync(ticket, cancellationToken);
        var dto = TicketMapper.ToDto(ticket, position);
        await _notifier.NotifyTicketUpdatedAsync(dto, cancellationToken);
        await NotifyFilaAsync(ticket.Especialidade, cancellationToken);
        return dto;
    }

    public async Task<TicketDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var ticket = await _store.GetTicketAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Ticket {id} não encontrado.");

        int? position = null;
        if (ticket.Status == TicketStatus.Aguardando)
        {
            position = await CalculatePositionAsync(ticket, cancellationToken);
        }

        return TicketMapper.ToDto(ticket, position);
    }

    public async Task RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var ticket = await _store.GetTicketAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Ticket {id} não encontrado.");

        var especialidade = ticket.Especialidade;
        var removed = await _store.RemoveTicketAsync(id, cancellationToken);
        if (!removed)
        {
            throw new NotFoundException($"Ticket {id} não encontrado.");
        }

        _logger.LogInformation(
            "Ticket removed. TicketId={TicketId} CodigoSenha={CodigoSenha} Especialidade={Especialidade} PreviousStatus={Status}",
            ticket.Id, ticket.CodigoSenha, especialidade, ticket.Status);

        await _notifier.NotifyTicketUpdatedAsync(TicketMapper.ToDto(ticket), cancellationToken);
        await NotifyFilaAsync(especialidade, cancellationToken);
    }

    private async Task<int?> CalculatePositionAsync(
        Domain.Entities.Ticket ticket,
        CancellationToken cancellationToken)
    {
        if (ticket.Status != TicketStatus.Aguardando)
        {
            return null;
        }

        var waiting = await _store.GetWaitingAsync(ticket.Especialidade, cancellationToken);
        var consecutive = await _store.GetConsecutivePreferentialAsync(
            _clock.Today, ticket.Especialidade, cancellationToken);

        return QueuePrioritySelector.GetPosition(
            ticket.Id,
            waiting,
            consecutive,
            _queueOptions.PreferentialCallsBeforeNormal);
    }

    internal async Task NotifyFilaAsync(Especialidade especialidade, CancellationToken cancellationToken)
    {
        var fila = await BuildFilaDtosAsync(especialidade, cancellationToken);
        await _notifier.NotifyFilaUpdatedAsync(especialidade, fila, cancellationToken);
    }

    internal async Task<IReadOnlyList<TicketDto>> BuildFilaDtosAsync(
        Especialidade especialidade,
        CancellationToken cancellationToken)
    {
        var waiting = await _store.GetWaitingAsync(especialidade, cancellationToken);
        var consecutive = await _store.GetConsecutivePreferentialAsync(
            _clock.Today, especialidade, cancellationToken);
        var ordered = QueuePrioritySelector.OrderWaiting(
            waiting,
            consecutive,
            _queueOptions.PreferentialCallsBeforeNormal);

        return ordered
            .Select((t, i) => TicketMapper.ToDto(t, i + 1))
            .ToList();
    }
}
