using FluentAssertions;
using QueueMed.Application.Abstractions;
using QueueMed.Domain.Enums;

namespace QueueMed.Application.Tests;

/// <summary>
/// Documents daily password sequence reset via IClock.Today boundary.
/// </summary>
public class SequenciaSenhaLogicTests
{
    private sealed class FakeClock : IClock
    {
        public FakeClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; set; }
        public DateOnly Today => DateOnly.FromDateTime(UtcNow);
    }

    [Fact]
    public void SequenceKey_ChangesWhenDayChanges()
    {
        var clock = new FakeClock(new DateTime(2026, 8, 4, 23, 59, 0, DateTimeKind.Utc));
        var day1 = (clock.Today, TipoAtendimento.Preferencial);

        clock.UtcNow = new DateTime(2026, 8, 5, 0, 1, 0, DateTimeKind.Utc);
        var day2 = (clock.Today, TipoAtendimento.Preferencial);

        day1.Should().NotBe(day2);
        day1.Item1.Should().Be(new DateOnly(2026, 8, 4));
        day2.Item1.Should().Be(new DateOnly(2026, 8, 5));
    }

    [Theory]
    [InlineData(TipoAtendimento.Preferencial, 1, "P001")]
    [InlineData(TipoAtendimento.Preferencial, 12, "P012")]
    [InlineData(TipoAtendimento.Normal, 1, "N001")]
    [InlineData(TipoAtendimento.Normal, 105, "N105")]
    public void CodigoFormat_MatchesPrefixAndPadding(TipoAtendimento tipo, int numero, string expected)
    {
        var prefix = tipo == TipoAtendimento.Preferencial ? "P" : "N";
        var codigo = $"{prefix}{numero:D3}";
        codigo.Should().Be(expected);
    }
}
