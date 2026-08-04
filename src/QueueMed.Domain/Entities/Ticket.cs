using QueueMed.Domain.Enums;

namespace QueueMed.Domain.Entities;

public class Ticket
{
    public Guid Id { get; set; }
    public string CodigoSenha { get; set; } = string.Empty;
    public TipoAtendimento TipoAtendimento { get; set; }
    public Especialidade Especialidade { get; set; }
    public TicketStatus Status { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? ChamadoEm { get; set; }
    public DateTime? ConcluidoEm { get; set; }
}
