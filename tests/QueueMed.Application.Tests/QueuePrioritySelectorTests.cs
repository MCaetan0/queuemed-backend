using FluentAssertions;
using QueueMed.Application.Services;
using QueueMed.Domain.Entities;
using QueueMed.Domain.Enums;

namespace QueueMed.Application.Tests;

public class QueuePrioritySelectorTests
{
    private static Ticket Ticket(string code, TipoAtendimento tipo, DateTime created) => new()
    {
        Id = Guid.NewGuid(),
        CodigoSenha = code,
        TipoAtendimento = tipo,
        Especialidade = Especialidade.Clinico,
        Status = TicketStatus.Aguardando,
        CriadoEm = created
    };

    [Fact]
    public void SelectNext_OnlyPreferential_ReturnsOldestPreferential()
    {
        var baseTime = new DateTime(2026, 8, 4, 10, 0, 0, DateTimeKind.Utc);
        var waiting = new[]
        {
            Ticket("P002", TipoAtendimento.Preferencial, baseTime.AddMinutes(2)),
            Ticket("P001", TipoAtendimento.Preferencial, baseTime)
        };

        var next = QueuePrioritySelector.SelectNext(waiting, consecutivePreferential: 0, preferentialCallsBeforeNormal: 2);

        next!.CodigoSenha.Should().Be("P001");
    }

    [Fact]
    public void SelectNext_OnlyNormal_ReturnsOldestNormal()
    {
        var baseTime = new DateTime(2026, 8, 4, 10, 0, 0, DateTimeKind.Utc);
        var waiting = new[]
        {
            Ticket("N002", TipoAtendimento.Normal, baseTime.AddMinutes(1)),
            Ticket("N001", TipoAtendimento.Normal, baseTime)
        };

        var next = QueuePrioritySelector.SelectNext(waiting, 5, 2);

        next!.CodigoSenha.Should().Be("N001");
    }

    [Fact]
    public void SelectNext_Mixed_WithNEquals2_CallsPreferentialThenPreferentialThenNormal()
    {
        var baseTime = new DateTime(2026, 8, 4, 10, 0, 0, DateTimeKind.Utc);
        var waiting = new List<Ticket>
        {
            Ticket("P001", TipoAtendimento.Preferencial, baseTime),
            Ticket("P002", TipoAtendimento.Preferencial, baseTime.AddMinutes(1)),
            Ticket("N001", TipoAtendimento.Normal, baseTime.AddMinutes(2)),
            Ticket("P003", TipoAtendimento.Preferencial, baseTime.AddMinutes(3)),
            Ticket("N002", TipoAtendimento.Normal, baseTime.AddMinutes(4))
        };

        var consecutive = 0;
        var called = new List<string>();

        while (waiting.Count > 0)
        {
            var next = QueuePrioritySelector.SelectNext(waiting, consecutive, 2)!;
            called.Add(next.CodigoSenha);
            consecutive = QueuePrioritySelector.NextConsecutiveCount(next, consecutive);
            waiting.Remove(next);
        }

        called.Should().Equal("P001", "P002", "N001", "P003", "N002");
    }

    [Fact]
    public void SelectNext_WhenConsecutiveReached_IntercalatesNormalEvenIfPreferentialOlder()
    {
        var baseTime = new DateTime(2026, 8, 4, 10, 0, 0, DateTimeKind.Utc);
        var waiting = new[]
        {
            Ticket("P001", TipoAtendimento.Preferencial, baseTime),
            Ticket("N001", TipoAtendimento.Normal, baseTime.AddMinutes(10))
        };

        var next = QueuePrioritySelector.SelectNext(waiting, consecutivePreferential: 2, preferentialCallsBeforeNormal: 2);

        next!.CodigoSenha.Should().Be("N001");
    }

    [Fact]
    public void SelectNext_WhenNormalsExhausted_ContinuesPreferential()
    {
        var baseTime = new DateTime(2026, 8, 4, 10, 0, 0, DateTimeKind.Utc);
        var waiting = new[]
        {
            Ticket("P001", TipoAtendimento.Preferencial, baseTime),
            Ticket("P002", TipoAtendimento.Preferencial, baseTime.AddMinutes(1))
        };

        var next = QueuePrioritySelector.SelectNext(waiting, consecutivePreferential: 10, preferentialCallsBeforeNormal: 2);

        next!.CodigoSenha.Should().Be("P001");
    }

    [Fact]
    public void SelectNext_Empty_ReturnsNull()
    {
        QueuePrioritySelector.SelectNext([], 0, 2).Should().BeNull();
    }

    [Fact]
    public void NextConsecutiveCount_ResetsOnNormal()
    {
        var preferential = Ticket("P001", TipoAtendimento.Preferencial, DateTime.UtcNow);
        var normal = Ticket("N001", TipoAtendimento.Normal, DateTime.UtcNow);

        QueuePrioritySelector.NextConsecutiveCount(preferential, 1).Should().Be(2);
        QueuePrioritySelector.NextConsecutiveCount(normal, 2).Should().Be(0);
    }

    [Fact]
    public void OrderWaiting_AssignsDynamicPositionsMatchingCallOrder()
    {
        var baseTime = new DateTime(2026, 8, 4, 10, 0, 0, DateTimeKind.Utc);
        var p1 = Ticket("P001", TipoAtendimento.Preferencial, baseTime);
        var n1 = Ticket("N001", TipoAtendimento.Normal, baseTime.AddMinutes(1));
        var p2 = Ticket("P002", TipoAtendimento.Preferencial, baseTime.AddMinutes(2));

        var ordered = QueuePrioritySelector.OrderWaiting([p1, n1, p2], consecutivePreferential: 0, preferentialCallsBeforeNormal: 2);

        ordered.Select(t => t.CodigoSenha).Should().Equal("P001", "P002", "N001");
        QueuePrioritySelector.GetPosition(n1.Id, [p1, n1, p2], 0, 2).Should().Be(3);
        QueuePrioritySelector.GetPosition(p1.Id, [p1, n1, p2], 0, 2).Should().Be(1);
    }

    [Fact]
    public void GetPosition_UnknownTicket_ReturnsNull()
    {
        var waiting = new[] { Ticket("P001", TipoAtendimento.Preferencial, DateTime.UtcNow) };
        QueuePrioritySelector.GetPosition(Guid.NewGuid(), waiting, 0, 2).Should().BeNull();
    }
}
