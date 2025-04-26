using CryptoRates.UI.API.Extensions;
using CryptoRates.UI.API.Services.Contracts;

namespace CryptoRates.UI.API.Endpoints;

public static class SymbolsEndpoints
{
    public static WebApplication AddSymbolsEndpoints(this WebApplication app)
    {
        app.MapGet("api/symbols", async (ISymbolsService service) =>
        {
            var symbols = await service.GetSymbolsAsync();
            return symbols.ToApiResult();
        }).WithTags("Symbols");

        return app;
    }
}