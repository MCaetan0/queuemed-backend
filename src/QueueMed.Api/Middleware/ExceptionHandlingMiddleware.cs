using System.Net;
using System.Text.Json;
using FluentValidation;
using QueueMed.Application.Exceptions;

namespace QueueMed.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var (status, message) = exception switch
        {
            NotFoundException e => (HttpStatusCode.NotFound, e.Message),
            ConflictException e => (HttpStatusCode.Conflict, e.Message),
            ValidationAppException e => (HttpStatusCode.BadRequest, e.Message),
            UnauthorizedAppException e => (HttpStatusCode.Unauthorized, e.Message),
            ValidationException e => (HttpStatusCode.BadRequest,
                string.Join("; ", e.Errors.Select(err => err.ErrorMessage))),
            _ => (HttpStatusCode.InternalServerError, "Erro interno do servidor.")
        };

        if (status == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception");
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)status;
        var payload = JsonSerializer.Serialize(new { error = message });
        await context.Response.WriteAsync(payload);
    }
}
