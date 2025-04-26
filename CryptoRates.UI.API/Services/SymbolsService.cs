using CryptoRates.UI.API.DataTransferObjects;
using CryptoRates.UI.API.ExternalServices.Contracts;
using CryptoRates.UI.API.Services.Contracts;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

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
        _cacheKey = configuration["Cache:CryptoSymbolsKey"] ?? "crypto-symbols";
        _cacheExpiration = TimeSpan.FromMinutes(
            configuration.GetValue<double>("Cache:ExpirationMinutes", 1440)); // Default 24 hours
    }

    public async Task<List<SymbolDto>> GetSymbolsAsync()
    {
        var cachedSymbols = await TryGetFromCacheAsync();
        if (cachedSymbols is not null && cachedSymbols.Count > 0)
            return cachedSymbols;

        var symbols = await _coinMarketCapService.FetchSymbolsAsync();
        await SetToCacheAsync(symbols);
        return symbols;
    }

    public async Task<List<string>> ValidateSymbolsAsync(List<string> symbols)
    {
        var symbolsDto = await GetSymbolsAsync();

        return symbols.Where(symbol => symbolsDto.Any(s => s.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase))).ToList();
    }

    private async Task SetToCacheAsync(List<SymbolDto> symbols)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _cacheExpiration
        };
        var json = JsonSerializer.Serialize(symbols);
        await _cache.SetStringAsync(_cacheKey, json, options);
    }

    private async Task<List<SymbolDto>?> TryGetFromCacheAsync()
    {
        var symbolJson = await _cache.GetStringAsync(_cacheKey);
        return string.IsNullOrEmpty(symbolJson)
            ? null
            : JsonSerializer.Deserialize<List<SymbolDto>>(symbolJson);
    }
}