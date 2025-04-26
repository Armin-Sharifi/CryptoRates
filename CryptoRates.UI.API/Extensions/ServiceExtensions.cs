using CryptoRates.UI.API.ExternalServices.Contracts;
using CryptoRates.UI.API.ExternalServices;
using CryptoRates.UI.API.Services.Contracts;
using CryptoRates.UI.API.Services;

namespace CryptoRates.UI.API.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Add Swagger Api Explorer
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        // You can use Redis or SQL Server for distributed caching
        services.AddDistributedMemoryCache();

        services.AddHttpClient();
        services.AddScoped<ICoinMarketCapService, CoinMarketCapService>();
        services.AddScoped<ISymbolsService, SymbolsService>();
        services.AddScoped<IExchangeRatesService, ExchangeRatesService>();
        services.AddScoped<IRateService, RateService>();
        services.AddScoped<IQuoteService, QuoteService>();
        return services;
    }
}