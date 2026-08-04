using QueueMed.Application.DTOs;
using QueueMed.Domain.Entities;
using QueueMed.Domain.Enums;

namespace QueueMed.Application.Services;

public static class TicketMapper
{
    public static TicketDto ToDto(Ticket ticket, int? posicaoFila = null) =>
        new(
            ticket.Id,
            ticket.CodigoSenha,
            ticket.TipoAtendimento,
            ticket.Especialidade,
            ticket.Status,
            posicaoFila,
            ticket.CriadoEm,
            ticket.ChamadoEm,
            ticket.ConcluidoEm);
}
