namespace QueueMed.Application.Options;

public class QueueOptions
{
    public const string SectionName = "Queue";

    public int PreferentialCallsBeforeNormal { get; set; } = 2;

    /// <summary>TTL for temporary queue keys in Redis (hours).</summary>
    public int DataTtlHours { get; set; } = 24;
}
