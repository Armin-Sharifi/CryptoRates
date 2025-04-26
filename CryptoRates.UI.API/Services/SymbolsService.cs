using CryptoRates.UI.API.DataTransferObjects;
using CryptoRates.UI.API.ExternalServices.Contracts;
using CryptoRates.UI.API.Services.Contracts;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using ErrorOr;

namespace CryptoRates.UI.API.Services;

public class SymbolsService : ISymbolsService
{
    private readonly IDistributedCache _cache;
    private readonly ICoinMarketCapService _coinMarketCapService;
    private readonly string _cacheKey;
    private readonly TimeSpan _cacheExpiration;

    public SymbolsService(IDistributedCache cache, ICoinMarketCapService coinMarketCapService, IConfiguration configuration)
    {
        _cache = cache;
        _coinMarketCapService = coinMarketCapService;
        _cacheKey = configuration["CacheSymbols:Key"] ?? "crypto-symbols";
        _cacheExpiration = TimeSpan.FromMinutes(
            configuration.GetValue<double>("CacheSymbols:ExpirationMinutes", 1440)); // Default 24 hours
    }

    public async Task<ErrorOr<List<CryptoSymbol>>> GetSymbolsAsync()
    {
        var cachedSymbols = await TryGetFromCacheAsync();
        if (cachedSymbols is not null && cachedSymbols.Count > 0)
            return cachedSymbols;

        var symbols = await _coinMarketCapService.FetchSymbolsAsync();

        if (symbols.IsError)
        {
            return symbols.Errors;
        }

        await SetToCacheAsync(symbols.Value);
        return symbols;
    }

    public async Task<ErrorOr<List<string>>> ValidateSymbolsAsync(List<string> symbols)
    {
        var symbolsDto = await GetSymbolsAsync();

        //var validatedSymbols = new List<string>();

        if (symbolsDto.IsError)
        {
            return symbolsDto.Errors;
        }

        //foreach (var symbol in symbols)
        //{
        //    var validatedSymbol = symbolsDto.Value.SingleOrDefault(x => x.Symbol == symbol);
        //    if (validatedSymbol is not )
        //    {
        //        validatedSymbols.Add();
        //    }
        //}

        return symbols.Where(symbol => symbolsDto.Value.Any(s => s.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase))).ToList();
    }

    private async Task SetToCacheAsync(List<CryptoSymbol> symbols)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _cacheExpiration
        };
        var json = JsonSerializer.Serialize(symbols);
        await _cache.SetStringAsync(_cacheKey, json, options);
    }

    private async Task<List<CryptoSymbol>?> TryGetFromCacheAsync()
    {
        var symbolJson = await _cache.GetStringAsync(_cacheKey);
        return string.IsNullOrEmpty(symbolJson)
            ? null
            : JsonSerializer.Deserialize<List<CryptoSymbol>>(symbolJson);
    }
}