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
    private readonly ILogger<SymbolsService> _logger;
    private readonly string _cacheKey;
    private readonly TimeSpan _cacheExpiration;

    public SymbolsService(
        IDistributedCache cache,
        ICoinMarketCapService coinMarketCapService,
        IConfiguration configuration,
        ILogger<SymbolsService> logger)
    {
        _cache = cache;
        _coinMarketCapService = coinMarketCapService;
        _logger = logger;
        _cacheKey = configuration["CacheSymbols:Key"] ?? "crypto-symbols";
        _cacheExpiration = TimeSpan.FromMinutes(
            configuration.GetValue<double>("CacheSymbols:ExpirationMinutes", 1440)); // Default 24 hours
    }

    public async Task<ErrorOr<List<CryptoSymbol>>> GetSymbolsAsync()
    {
        try
        {
            _logger.LogInformation("Getting crypto symbols");

            var cachedSymbols = await TryGetFromCacheAsync();
            if (cachedSymbols is not null && cachedSymbols.Count > 0)
            {
                _logger.LogInformation("Retrieved {Count} symbols from cache", cachedSymbols.Count);
                return cachedSymbols;
            }

            _logger.LogInformation("Fetching symbols from API");
            var symbols = await _coinMarketCapService.FetchSymbolsAsync();
            if (symbols.IsError)
            {
                _logger.LogError("Failed to fetch symbols: {Error}", symbols.Errors);
                return symbols.Errors;
            }

            await SetToCacheAsync(symbols.Value);
            return symbols;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting symbols");
            return Error.Unexpected("SymbolsService.GetSymbols.Error", ex.Message);
        }
    }

    public async Task<ErrorOr<List<string>>> ValidateSymbolsAsync(List<string> symbols)
    {
        try
        {
            _logger.LogInformation("Validating symbols");

            var symbolsDto = await GetSymbolsAsync();
            if (symbolsDto.IsError)
            {
                _logger.LogError("Failed to get symbols for validation: {Error}", symbolsDto.Errors);
                return symbolsDto.Errors;
            }

            var validSymbols = symbols.Where(symbol =>
                symbolsDto.Value.Any(s => s.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            _logger.LogInformation("Validated {Count} symbols", validSymbols.Count);
            return validSymbols;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating symbols");
            return Error.Unexpected("SymbolsService.ValidateSymbols.Error", ex.Message);
        }
    }

    private async Task SetToCacheAsync(List<CryptoSymbol> symbols)
    {
        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cacheExpiration
            };
            var json = JsonSerializer.Serialize(symbols);
            await _cache.SetStringAsync(_cacheKey, json, options);
            _logger.LogDebug("Symbols cached successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error caching symbols");
        }
    }

    private async Task<List<CryptoSymbol>?> TryGetFromCacheAsync()
    {
        try
        {
            var symbolJson = await _cache.GetStringAsync(_cacheKey);
            return string.IsNullOrEmpty(symbolJson)
                ? null
                : JsonSerializer.Deserialize<List<CryptoSymbol>>(symbolJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving symbols from cache");
            return null;
        }
    }
}