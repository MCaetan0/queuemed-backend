using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using QueueMed.Application.Services;
using QueueMed.Application.Validators;

namespace QueueMed.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateTicketRequestValidator>();
        services.AddScoped<TicketService>();
        services.AddScoped<QueueService>();
        services.AddScoped<AuthService>();
        return services;
    }
}
