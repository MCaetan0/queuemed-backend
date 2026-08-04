using FluentAssertions;
using QueueMed.Domain.Enums;

namespace QueueMed.Application.Tests;

/// <summary>
/// Documents the simplified ticket lifecycle: Aguardando → Chamado → remove.
/// </summary>
public class TicketLifecycleTests
{
    [Theory]
    [InlineData(TicketStatus.Aguardando)]
    [InlineData(TicketStatus.Chamado)]
    public void SupportedStatuses_AreOnlyWaitingAndCalled(TicketStatus status)
    {
        Enum.IsDefined(status).Should().BeTrue();
        ((int)status).Should().BeOneOf(1, 2);
    }

    [Fact]
    public void RemovedStatuses_AreNoLongerDefined()
    {
        Enum.GetNames<TicketStatus>().Should().BeEquivalentTo("Aguardando", "Chamado");
    }
}
