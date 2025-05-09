﻿using System.ComponentModel.DataAnnotations;
using CryptoRates.UI.API.Extensions;
using CryptoRates.UI.API.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace CryptoRates.UI.API.Endpoints;

public static class QuotesEndpoints
{
    public static WebApplication AddQuotesEndpoints(this WebApplication app)
    {
        app.MapGet("api/quotes", async ([FromServices] IQuoteService service, [FromQuery][Required] string symbols) =>
        {
            var symbolList = symbols.Split(',').ToList();

            var results = await service.GetPricesAsync(symbolList);
            return results.ToApiResult();
        }).WithTags("Quotes"); ;

        return app;
    }
}