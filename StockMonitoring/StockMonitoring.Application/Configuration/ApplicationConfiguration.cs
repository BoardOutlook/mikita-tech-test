using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StockMonitoring.Application.Services;
using StockMonitoring.Core.Interfaces.Services;

namespace StockMonitoring.Application.Configuration;

public static class ApplicationConfiguration
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IStockService, StockService>();
        services.AddMemoryCache();
        
        return services;
    }
}
