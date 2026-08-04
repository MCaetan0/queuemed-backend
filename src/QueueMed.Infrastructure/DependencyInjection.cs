using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QueueMed.Application.Abstractions;
using QueueMed.Application.Options;
using QueueMed.Infrastructure.Auth;
using QueueMed.Infrastructure.Redis;
using QueueMed.Infrastructure.Time;
using StackExchange.Redis;

namespace QueueMed.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<QueueOptions>(configuration.GetSection(QueueOptions.SectionName));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<QrCodeOptions>(configuration.GetSection(QrCodeOptions.SectionName));
        services.Configure<SeedOptions>(configuration.GetSection(SeedOptions.SectionName));

        var redisConnection = ResolveRedisConnectionString(configuration);
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));
        services.AddSingleton<IQueueStore, RedisQueueStore>();

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        return services;
    }

    /// <summary>
    /// Builds a StackExchange.Redis connection string from Redis__* env vars,
    /// or falls back to ConnectionStrings:Redis / REDIS_URL.
    /// </summary>
    public static string ResolveRedisConnectionString(IConfiguration configuration)
    {
        var host = configuration["Redis:Host"];
        if (!string.IsNullOrWhiteSpace(host))
        {
            host = host.Trim();
            var port = configuration["Redis:Port"]?.Trim();

            // host:port if Host has no port yet and Port is set
            var endpoint = host.Contains(':') || string.IsNullOrWhiteSpace(port)
                ? host
                : $"{host}:{port}";

            var parts = new List<string> { endpoint };

            var password = configuration["Redis:Password"];
            if (!string.IsNullOrWhiteSpace(password))
            {
                parts.Add($"password={password}");
            }

            var user = configuration["Redis:User"];
            if (!string.IsNullOrWhiteSpace(user))
            {
                parts.Add($"user={user}");
            }

            var abortConnect = configuration["Redis:AbortConnect"];
            parts.Add($"abortConnect={(string.IsNullOrWhiteSpace(abortConnect) ? "false" : abortConnect.Trim())}");

            var ssl = configuration["Redis:Ssl"];
            if (!string.IsNullOrWhiteSpace(ssl))
            {
                parts.Add($"ssl={ssl.Trim()}");
            }

            return string.Join(",", parts);
        }

        var readyMade = configuration.GetConnectionString("Redis")
                        ?? configuration["REDIS_URL"];

        if (string.IsNullOrWhiteSpace(readyMade))
        {
            throw new InvalidOperationException(
                "Redis não configurado. Defina Redis__Host (e opcionalmente Redis__Port, Redis__Password, Redis__User) " +
                "ou ConnectionStrings__Redis.");
        }

        return readyMade;
    }
}
