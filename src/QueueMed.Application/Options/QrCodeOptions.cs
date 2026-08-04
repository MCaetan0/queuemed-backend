namespace QueueMed.Application.Options;

public class QrCodeOptions
{
    public const string SectionName = "QrCode";

    public string EntryUrl { get; set; } = "http://localhost:3000/entrar";
}
