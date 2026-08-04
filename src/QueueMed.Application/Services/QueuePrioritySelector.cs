using QueueMed.Application.DTOs;
using QueueMed.Domain.Entities;
using QueueMed.Domain.Enums;

namespace QueueMed.Application.Services;

/// <summary>
/// Pure priority logic: after N consecutive preferential calls, intercalate 1 normal.
/// </summary>
public static class QueuePrioritySelector
{
    public static Ticket? SelectNext(
        IReadOnlyList<Ticket> waiting,
        int consecutivePreferential,
        int preferentialCallsBeforeNormal)
    {
        if (waiting.Count == 0)
        {
            return null;
        }

        var preferentials = waiting
            .Where(t => t.TipoAtendimento == TipoAtendimento.Preferencial)
            .OrderBy(t => t.CriadoEm)
            .ThenBy(t => t.Id)
            .ToList();

        var normals = waiting
            .Where(t => t.TipoAtendimento == TipoAtendimento.Normal)
            .OrderBy(t => t.CriadoEm)
            .ThenBy(t => t.Id)
            .ToList();

        if (preferentials.Count == 0)
        {
            return normals.FirstOrDefault();
        }

        if (normals.Count == 0)
        {
            return preferentials.FirstOrDefault();
        }

        if (consecutivePreferential >= preferentialCallsBeforeNormal)
        {
            return normals.First();
        }

        return preferentials.First();
    }

    public static int NextConsecutiveCount(Ticket called, int currentConsecutive)
    {
        return called.TipoAtendimento == TipoAtendimento.Preferencial
            ? currentConsecutive + 1
            : 0;
    }

    /// <summary>
    /// Simulates call order to produce the dynamic queue positions for waiting tickets.
    /// </summary>
    public static IReadOnlyList<Ticket> OrderWaiting(
        IReadOnlyList<Ticket> waiting,
        int consecutivePreferential,
        int preferentialCallsBeforeNormal)
    {
        var remaining = waiting.ToList();
        var ordered = new List<Ticket>(remaining.Count);
        var consecutive = consecutivePreferential;

        while (remaining.Count > 0)
        {
            var next = SelectNext(remaining, consecutive, preferentialCallsBeforeNormal);
            if (next is null)
            {
                break;
            }

            ordered.Add(next);
            remaining.Remove(next);
            consecutive = NextConsecutiveCount(next, consecutive);
        }

        return ordered;
    }

    public static int? GetPosition(
        Guid ticketId,
        IReadOnlyList<Ticket> waiting,
        int consecutivePreferential,
        int preferentialCallsBeforeNormal)
    {
        var ordered = OrderWaiting(waiting, consecutivePreferential, preferentialCallsBeforeNormal);
        for (var i = 0; i < ordered.Count; i++)
        {
            if (ordered[i].Id == ticketId)
            {
                return i + 1;
            }
        }

        return null;
    }
}
