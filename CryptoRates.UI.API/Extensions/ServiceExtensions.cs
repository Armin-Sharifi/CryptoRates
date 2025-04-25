namespace CryptoRates.UI.API.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Add Swagger Api Explorer
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        services.AddHttpClient(); 

        return services;
    }
}