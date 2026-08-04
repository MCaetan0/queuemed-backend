using System.Text.Json.Serialization;
using QueueMed.Domain.Enums;

namespace QueueMed.Application.DTOs;

public record CreateTicketRequest(
    [property: JsonConverter(typeof(JsonStringEnumConverter))] TipoAtendimento TipoAtendimento,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] Especialidade Especialidade);

public record ChamarProximoRequest(
    [property: JsonConverter(typeof(JsonStringEnumConverter))] Especialidade Especialidade);

public record LoginRequest(string Usuario, string Senha);

public record LoginResponse(string Token, string Nome, Guid AtendenteId);

public record TicketDto(
    Guid Id,
    string CodigoSenha,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] TipoAtendimento TipoAtendimento,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] Especialidade Especialidade,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] TicketStatus Status,
    int? PosicaoFila,
    DateTime CriadoEm,
    DateTime? ChamadoEm,
    DateTime? ConcluidoEm);

public record FilaResponse(
    [property: JsonConverter(typeof(JsonStringEnumConverter))] Especialidade Especialidade,
    IReadOnlyList<TicketDto> Tickets);
