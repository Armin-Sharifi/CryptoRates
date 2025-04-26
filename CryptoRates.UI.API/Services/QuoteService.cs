using CryptoRates.UI.API.DataTransferObjects;
using CryptoRates.UI.API.ExternalServices.Contracts;
using CryptoRates.UI.API.Services.Contracts;
using ErrorOr;

namespace CryptoRates.UI.API.Services;

public class QuoteService : IQuoteService
{
    private readonly ICoinMarketCapService _coinMarketCapService;
    private readonly IRateService _rateService;
    private readonly ISymbolsService _symbolsService;
    private readonly ILogger<QuoteService> _logger;
    private readonly string _baseExchangeCurrency;

    public QuoteService(ICoinMarketCapService coinMarketCapService,
        IRateService rateService,
        IConfiguration configuration,
        ISymbolsService symbolsService,
        ILogger<QuoteService> logger)
    {
        _coinMarketCapService = coinMarketCapService;
        _rateService = rateService;
        _symbolsService = symbolsService;
        _logger = logger;
        _baseExchangeCurrency = configuration["ExchangeRates:BaseCurrency"] ?? "EUR";
    }

    public async Task<ErrorOr<List<QuoteResult>>> GetPricesAsync(List<string> symbols)
    {
        _logger.LogInformation("Getting prices for {Count} symbols", symbols?.Count ?? 0);

        if (symbols == null || symbols.Count == 0)
        {
            _logger.LogWarning("No symbols provided to GetPricesAsync");
            return Error.Validation("Symbols", "At least one symbol must be provided");
        }

        try
        {
            symbols = symbols.Select(symbol => symbol.ToUpper()).ToList();
            _logger.LogDebug("Validating symbols: {Symbols}", string.Join(", ", symbols));

            var validatedSymbols = await _symbolsService.ValidateSymbolsAsync(symbols);
            if (validatedSymbols.IsError)
            {
                _logger.LogWarning("Symbol validation failed: {Errors}", string.Join(",", validatedSymbols.Errors));
                return validatedSymbols.Errors;
            }

            if (validatedSymbols.Value.Count == 0)
            {
                return Error.Validation("QuoteService.ValidateSymbols.Error", "The symbols you provided are not valid.");
            }

            _logger.LogInformation("Fetching quotes for {Count} validated symbols", validatedSymbols.Value.Count);
            var quotes = await _coinMarketCapService.GetLatestQuotesAsync(validatedSymbols.Value);
            if (quotes.IsError)
            {
                _logger.LogError("Failed to fetch quotes: {Errors}", string.Join(", ", quotes.Errors));
                return quotes.Errors;
            }

            _logger.LogInformation("Fetching exchange rates");
            var rates = await _rateService.GetRatesAsync();
            if (rates.IsError)
            {
                _logger.LogError("Failed to fetch exchange rates: {Errors}", string.Join(", ", rates.Errors));
                return rates.Errors;
            }

            _logger.LogInformation("Fetching symbol details");
            var symbolsDtos = await _symbolsService.GetSymbolsAsync();
            if (symbolsDtos.IsError)
            {
                _logger.LogError("Failed to fetch symbol details: {Errors}", string.Join(", ", symbolsDtos.Errors));
                return symbolsDtos.Errors;
            }

            var results = new List<QuoteResult>();
            foreach (var symbol in validatedSymbols.Value)
            {
                var quote = quotes.Value.FirstOrDefault(q => q.Symbol == symbol);
                if (quote is null)
                {
                    _logger.LogWarning("No quote found for symbol: {Symbol}", symbol);
                    continue;
                }

                // Adding Base Currency
                var prices = new List<Price>
                {
                    new(_baseExchangeCurrency, quote.Price)
                };

                prices.AddRange(rates.Value.Select(rate => new Price(rate.Icon, quote.Price * rate.Rate)));
                results.Add(new QuoteResult(symbolsDtos.Value.SingleOrDefault(x => x.Symbol == symbol), prices));
                _logger.LogDebug("Added quote result for {Symbol}", symbol);
            }

            _logger.LogInformation("Successfully processed {Count} quotes", results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in GetPricesAsync");
            return Error.Unexpected("QuoteService.GetPrices.UnexpectedError", ex.Message);
        }
    }
}