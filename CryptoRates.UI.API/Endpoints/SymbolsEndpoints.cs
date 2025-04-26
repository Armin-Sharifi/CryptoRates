using CryptoRates.UI.API.ExternalServices.Contracts;
using CryptoRates.UI.API.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace CryptoRates.UI.API.Endpoints;

public static class SymbolsEndpoints
{
    public static WebApplication AddSymbolsEndpoints(this WebApplication app)
    {
        app.MapGet("api/symbols", async (ISymbolsService service) =>
        {
            var symbols = await service.GetSymbolsAsync();
            return Results.Ok(symbols);
        }).WithTags("Symbols");

        return app;
    }
}