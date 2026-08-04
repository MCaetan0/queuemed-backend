using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QueueMed.Application.DTOs;
using QueueMed.Application.Services;

namespace QueueMed.Api.Controllers;

[ApiController]
[Route("tickets")]
public class TicketsController : ControllerBase
{
    private readonly TicketService _ticketService;
    private readonly IValidator<CreateTicketRequest> _createValidator;

    public TicketsController(
        TicketService ticketService,
        IValidator<CreateTicketRequest> createValidator)
    {
        _ticketService = ticketService;
        _createValidator = createValidator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(TicketDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateTicketRequest request, CancellationToken cancellationToken)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);
        var ticket = await _ticketService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = ticket.Id }, ticket);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TicketDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TicketDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var ticket = await _ticketService.GetByIdAsync(id, cancellationToken);
        return Ok(ticket);
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Remove(Guid id, CancellationToken cancellationToken)
    {
        await _ticketService.RemoveAsync(id, cancellationToken);
        return NoContent();
    }
}
