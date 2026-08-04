namespace QueueMed.Application.Options;

public class SeedOptions
{
    public const string SectionName = "Seed";

    public string AtendenteUsuario { get; set; } = "atendente";
    public string AtendenteSenha { get; set; } = "Atendente@123";
    public string AtendenteNome { get; set; } = "Atendente Padrão";
}
