using CryptoRates.UI.API.DataTransferObjects;
using CryptoRates.UI.API.ExternalServices.Contracts;
using CryptoRates.UI.API.Services.Contracts;
using ErrorOr;
using Microsoft.Extensions.Caching.Distributed;

namespace CryptoRates.UI.API.Services;

public class QuoteService : IQuoteService
{
    private readonly ICoinMarketCapService _coinMarketCapService;
    private readonly IRateService _rateService;
    private readonly ISymbolsService _symbolsService;
    private readonly string _baseExchangeCurrency;

    public QuoteService(IDistributedCache cache, ICoinMarketCapService coinMarketCapService, IRateService rateService, IConfiguration configuration, ISymbolsService symbolsService)
    {
        _coinMarketCapService = coinMarketCapService;
        _rateService = rateService;
        _symbolsService = symbolsService;
        _baseExchangeCurrency = configuration["ExchangeRates:BaseCurrency"] ?? "EUR";
    }

    public async Task<ErrorOr<List<QuoteResult>>> GetPricesAsync(List<string> symbols)
    {
        if (symbols.Count == 0)
        {
            return Error.Validation("Symbols", "At least one symbol must be provided");
        }

        symbols = symbols.Select(symbol => symbol.ToUpper()).ToList();

        var validatedSymbols = await _symbolsService.ValidateSymbolsAsync(symbols);
        if (validatedSymbols.IsError)
        {
            return validatedSymbols.Errors;
        }

        var quotes = await _coinMarketCapService.GetLatestQuotesAsync(validatedSymbols.Value);
        if (quotes.IsError)
        {
            return quotes.Errors;
        }

        var rates = await _rateService.GetRatesAsync();
        if (rates.IsError)
        {
            return rates.Errors;
        }

        var symbolsDtos = await _symbolsService.GetSymbolsAsync();
        if (symbolsDtos.IsError)
        {
            return symbolsDtos.Errors;
        }

        var results = new List<QuoteResult>();

        foreach (var symbol in validatedSymbols.Value)
        {
            var quote = quotes.Value.FirstOrDefault(q => q.Symbol == symbol);
            if (quote is null)
                continue;

            //Adding Base Currency
            var prices = new List<Price>
            {
                new(_baseExchangeCurrency, quote.Price)
            };

            prices.AddRange(rates.Value.Select(rate => new Price(rate.Icon, quote.Price * rate.Rate)));

            results.Add(new QuoteResult(symbolsDtos.Value.SingleOrDefault(x => x.Symbol == symbol), prices));
        }

        return results;
    }
}