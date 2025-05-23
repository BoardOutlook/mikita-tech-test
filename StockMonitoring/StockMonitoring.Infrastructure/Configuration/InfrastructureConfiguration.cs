using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StockMonitoring.Core.Interfaces.Clients;
using StockMonitoring.Infrastructure.Clients;

namespace StockMonitoring.Infrastructure.Configuration;

public static class InfrastructureConfiguration
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ApiOptions>(configuration.GetSection(ApiOptions.SectionName));
        services.AddHttpClientConfiguration<IStockClient, StockClient>(configuration);
        
        return services;
    }
}
