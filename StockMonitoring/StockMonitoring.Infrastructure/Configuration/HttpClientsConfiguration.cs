using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;

namespace StockMonitoring.Infrastructure.Configuration;

internal static class HttpClientsConfiguration
{
    public static IServiceCollection AddHttpClientConfiguration<TClient, TImplementation>(this IServiceCollection services,
        IConfiguration configuration)
        where TClient : class
        where TImplementation : class, TClient
    {
        if (!int.TryParse(configuration["ClientConfiguration:DefaultEventsAllowedBeforeBreaking"], out var defaultEventsAllowed) || defaultEventsAllowed <= 0)
        {
            defaultEventsAllowed = 3; // Default value if not configured
        }

        services.AddHttpClient<TClient, TImplementation>(ConfigureClient)
            .AddTransientHttpErrorPolicy(builder => builder.WaitAndRetryAsync(new[]
            {
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(10)
            }))
            .AddTransientHttpErrorPolicy(builder => builder.CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: defaultEventsAllowed,
                durationOfBreak: TimeSpan.FromSeconds(30)
            ));

        return services;
    }

    private static void ConfigureClient(IServiceProvider provider, HttpClient client)
    {
        var apiOptions = provider.GetRequiredService<IOptions<ApiOptions>>().Value;

        if (string.IsNullOrEmpty(apiOptions.BaseUrl))
        {
            throw new InvalidOperationException("BaseUrl is not configured for the API");
        }

        client.BaseAddress = new Uri(apiOptions.BaseUrl);
        client.DefaultRequestHeaders.Add("Authorization", apiOptions.AuthCode);
        client.Timeout = TimeSpan.FromMinutes(5);
    }
}
