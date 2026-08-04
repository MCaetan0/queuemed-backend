namespace QueueMed.Domain.Entities;

public class Atendente
{
    public Guid Id { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; set; }
}
