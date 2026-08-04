using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QueueMed.Application.DTOs;
using QueueMed.Application.Services;
using QueueMed.Domain.Enums;

namespace QueueMed.Api.Controllers;

[ApiController]
[Route("fila")]
[Authorize]
public class FilaController : ControllerBase
{
    private readonly QueueService _queueService;
    private readonly IValidator<ChamarProximoRequest> _validator;

    public FilaController(QueueService queueService, IValidator<ChamarProximoRequest> validator)
    {
        _queueService = queueService;
        _validator = validator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(FilaResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<FilaResponse>> GetFila(
        [FromQuery] Especialidade especialidade,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(especialidade))
        {
            return BadRequest("Especialidade inválida.");
        }

        var fila = await _queueService.GetFilaAsync(especialidade, cancellationToken);
        return Ok(fila);
    }

    [HttpPost("chamar-proximo")]
    [ProducesResponseType(typeof(TicketDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TicketDto>> ChamarProximo(
        [FromBody] ChamarProximoRequest request,
        CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);
        var ticket = await _queueService.CallNextAsync(request.Especialidade, cancellationToken);
        return Ok(ticket);
    }
}
