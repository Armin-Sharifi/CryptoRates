using CryptoRates.UI.API.Services.Contracts;

namespace CryptoRates.UI.API.Endpoints;

public static class SymbolsEndpoints
{
    public static WebApplication AddSymbolsEndpoints(this WebApplication app)
    {
        app.MapGet("/symbols", async (ISymbolsRepository service) =>
        {
            var symbols = await service.GetSymbolsAsync();
            return Results.Ok(symbols);
        });

        return app;
    }
}