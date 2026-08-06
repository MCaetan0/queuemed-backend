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
        services.Configure<BaseUrlOptions>(configuration.GetSection(BaseUrlOptions.SectionName));
        services.Configure<QrCodeOptions>(configuration.GetSection(QrCodeOptions.SectionName));
        services.Configure<SeedOptions>(configuration.GetSection(SeedOptions.SectionName));

        var baseUrl = ResolveRequiredBaseUrl(configuration);

        services.PostConfigure<QrCodeOptions>(qr =>
        {
            if (!string.IsNullOrWhiteSpace(qr.EntryUrl))
            {
                qr.EntryUrl = qr.EntryUrl.Trim();
                return;
            }

            qr.EntryUrl = $"{baseUrl}/entrar";
        });

        var redisConnection = ResolveRedisConnectionString(configuration);
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));
        services.AddSingleton<IQueueStore, RedisQueueStore>();

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        return services;
    }

    /// <summary>Requires Base__Url. Throws if missing.</summary>
    public static string ResolveRequiredBaseUrl(IConfiguration configuration)
    {
        var baseUrl = configuration["Base:Url"]?.Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException(
                "Base__Url não configurado. Defina a variável de ambiente Base__Url.");
        }

        return baseUrl;
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

            string endpoint;
            if (host.Contains(':'))
            {
                endpoint = host;
            }
            else if (!string.IsNullOrWhiteSpace(port))
            {
                endpoint = $"{host}:{port}";
            }
            else
            {
                throw new InvalidOperationException(
                    "Redis__Port não configurado. Defina Redis__Port ou inclua a porta em Redis__Host.");
            }

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
            if (string.IsNullOrWhiteSpace(abortConnect))
            {
                throw new InvalidOperationException(
                    "Redis__AbortConnect não configurado. Defina Redis__AbortConnect (true/false).");
            }

            parts.Add($"abortConnect={abortConnect.Trim()}");

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
                "Redis não configurado. Defina Redis__Host e Redis__Port (e Redis__Password/Redis__User se necessário) " +
                "ou ConnectionStrings__Redis.");
        }

        return readyMade;
    }
}
