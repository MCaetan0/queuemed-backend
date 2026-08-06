namespace QueueMed.Application.Options;

public class QrCodeOptions
{
    public const string SectionName = "QrCode";

    /// <summary>
    /// Absolute URL encoded in the QR. If empty, becomes {Base:Url}/entrar.
    /// Env: QrCode__EntryUrl
    /// </summary>
    public string EntryUrl { get; set; } = string.Empty;
}
