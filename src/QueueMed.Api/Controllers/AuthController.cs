using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using QueueMed.Application.DTOs;
using QueueMed.Application.Services;

namespace QueueMed.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly IValidator<LoginRequest> _validator;

    public AuthController(AuthService authService, IValidator<LoginRequest> validator)
    {
        _authService = authService;
        _validator = validator;
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);
        var response = await _authService.LoginAsync(request, cancellationToken);
        return Ok(response);
    }
}
