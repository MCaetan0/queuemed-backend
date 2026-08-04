using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using QueueMed.Application.Abstractions;
using QueueMed.Application.Options;
using QueueMed.Domain.Entities;
using QueueMed.Domain.Enums;
using StackExchange.Redis;

namespace QueueMed.Infrastructure.Redis;

public sealed class RedisQueueStore : IQueueStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IDatabase _db;
    private readonly TimeSpan _ttl;

    public RedisQueueStore(IConnectionMultiplexer multiplexer, IOptions<QueueOptions> queueOptions)
    {
        _db = multiplexer.GetDatabase();
        var hours = Math.Max(1, queueOptions.Value.DataTtlHours);
        _ttl = TimeSpan.FromHours(hours);
    }

    public async Task<long> IncrementSequenceAsync(
        DateOnly date,
        TipoAtendimento tipo,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = SequenceKey(date, tipo);
        var value = await _db.StringIncrementAsync(key);
        await _db.KeyExpireAsync(key, _ttl);
        return value;
    }

    public async Task SaveNewTicketAsync(Ticket ticket, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var ticketKey = TicketKey(ticket.Id);
        var waitingKey = WaitingKey(ticket.Especialidade);

        var tran = _db.CreateTransaction();
        _ = tran.StringSetAsync(ticketKey, Serialize(ticket), _ttl);
        _ = tran.SetAddAsync(waitingKey, ticket.Id.ToString());
        _ = tran.KeyExpireAsync(waitingKey, _ttl);

        if (!await tran.ExecuteAsync())
        {
            throw new InvalidOperationException("Falha ao salvar ticket no Redis.");
        }
    }

    public async Task<Ticket?> GetTicketAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var value = await _db.StringGetAsync(TicketKey(id));
        return value.IsNullOrEmpty ? null : Deserialize(value!);
    }

    public async Task<IReadOnlyList<Ticket>> GetWaitingAsync(
        Especialidade especialidade,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var ids = await _db.SetMembersAsync(WaitingKey(especialidade));
        if (ids.Length == 0)
        {
            return [];
        }

        var tickets = new List<Ticket>(ids.Length);
        foreach (var idValue in ids)
        {
            if (!Guid.TryParse(idValue, out var id))
            {
                continue;
            }

            var ticket = await GetTicketAsync(id, cancellationToken);
            if (ticket is not null && ticket.Status == TicketStatus.Aguardando)
            {
                tickets.Add(ticket);
            }
        }

        return tickets;
    }

    public async Task<int> GetConsecutivePreferentialAsync(
        DateOnly date,
        Especialidade especialidade,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var value = await _db.StringGetAsync(CounterKey(date, especialidade));
        return value.IsNullOrEmpty ? 0 : (int)value;
    }

    public async Task MarkCalledAsync(
        Ticket ticket,
        DateOnly date,
        int consecutivePreferential,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var ticketKey = TicketKey(ticket.Id);
        var waitingKey = WaitingKey(ticket.Especialidade);
        var chamadoKey = ChamadoKey(ticket.Especialidade);
        var counterKey = CounterKey(date, ticket.Especialidade);

        var tran = _db.CreateTransaction();
        _ = tran.StringSetAsync(ticketKey, Serialize(ticket), _ttl);
        _ = tran.SetRemoveAsync(waitingKey, ticket.Id.ToString());
        _ = tran.SetAddAsync(chamadoKey, ticket.Id.ToString());
        _ = tran.KeyExpireAsync(chamadoKey, _ttl);
        _ = tran.StringSetAsync(counterKey, consecutivePreferential, _ttl);

        if (!await tran.ExecuteAsync())
        {
            throw new InvalidOperationException("Falha ao marcar ticket como chamado no Redis.");
        }
    }

    public async Task<bool> RemoveTicketAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var ticket = await GetTicketAsync(id, cancellationToken);
        if (ticket is null)
        {
            return false;
        }

        var ticketKey = TicketKey(id);
        var waitingKey = WaitingKey(ticket.Especialidade);
        var chamadoKey = ChamadoKey(ticket.Especialidade);
        var idStr = id.ToString();

        var tran = _db.CreateTransaction();
        _ = tran.KeyDeleteAsync(ticketKey);
        _ = tran.SetRemoveAsync(waitingKey, idStr);
        _ = tran.SetRemoveAsync(chamadoKey, idStr);

        await tran.ExecuteAsync();
        return true;
    }

    private static string TicketKey(Guid id) => $"qm:ticket:{id}";

    private static string WaitingKey(Especialidade especialidade) =>
        $"qm:fila:{especialidade}:waiting";

    private static string ChamadoKey(Especialidade especialidade) =>
        $"qm:fila:{especialidade}:chamado";

    private static string SequenceKey(DateOnly date, TipoAtendimento tipo) =>
        $"qm:seq:{date:yyyy-MM-dd}:{tipo}";

    private static string CounterKey(DateOnly date, Especialidade especialidade) =>
        $"qm:counter:{date:yyyy-MM-dd}:{especialidade}";

    private static string Serialize(Ticket ticket) =>
        JsonSerializer.Serialize(ticket, JsonOptions);

    private static Ticket Deserialize(string json) =>
        JsonSerializer.Deserialize<Ticket>(json, JsonOptions)
        ?? throw new InvalidOperationException("Ticket Redis inválido.");
}
