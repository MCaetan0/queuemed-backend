namespace QueueMed.Application.Options;

public class BaseUrlOptions
{
    public const string SectionName = "Base";

    /// <summary>Public base URL. Env: Base__Url (required).</summary>
    public string Url { get; set; } = string.Empty;
}
