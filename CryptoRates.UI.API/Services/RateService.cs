using CryptoRates.UI.API.DataTransferObjects;
using CryptoRates.UI.API.ExternalServices.Contracts;
using CryptoRates.UI.API.Services.Contracts;
using ErrorOr;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace CryptoRates.UI.API.Services
{
    public class RateService : IRateService
    {
        private readonly IDistributedCache _cache;
        private readonly IExchangeRatesService _exchangeRatesService;
        private readonly ILogger<RateService> _logger;
        private readonly string _cacheKey;
        private readonly TimeSpan _cacheExpiration;

        public RateService(
            IDistributedCache cache,
            IExchangeRatesService exchangeRatesService,
            IConfiguration configuration,
            ILogger<RateService> logger)
        {
            _cache = cache;
            _exchangeRatesService = exchangeRatesService;
            _logger = logger;
            _cacheKey = configuration["CacheRates:CryptoSymbolsKey"] ?? "exchange-rates";
            _cacheExpiration = TimeSpan.FromMinutes(
                configuration.GetValue<double>("CacheRates:ExpirationMinutes", 60)); // Default 1 hour

            _logger.LogInformation("RateService initialized with cache key: {CacheKey}, expiration: {Expiration} minutes",
                _cacheKey, _cacheExpiration.TotalMinutes);
        }

        public async Task<ErrorOr<List<ExchangeRate>>> GetRatesAsync()
        {
            _logger.LogDebug("Getting exchange rates");

            try
            {
                // Try getting from cache first
                var cachedRates = await TryGetFromCacheAsync();
                if (cachedRates is not null && cachedRates.Count > 0)
                {
                    _logger.LogInformation("Retrieved {Count} exchange rates from cache", cachedRates.Count);
                    return cachedRates;
                }

                _logger.LogDebug("No valid rates found in cache, fetching from API");

                // Fetch rates from API
                var rates = await _exchangeRatesService.GetRates();
                if (rates.IsError)
                {
                    _logger.LogError("Failed to fetch exchange rates: {Errors}",
                        string.Join(", ", rates.Errors.Select(e => e.Description)));
                    return rates.Errors;
                }

                _logger.LogInformation("Successfully fetched {Count} exchange rates from API", rates.Value.Count);

                await SetToCacheAsync(rates.Value);

                return rates;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetRatesAsync");
                return Error.Unexpected("RateService.GetRates.UnexpectedError", ex.Message);
            }
        }

        private async Task SetToCacheAsync(List<ExchangeRate> rates)
        {
            try
            {
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _cacheExpiration
                };

                var json = JsonSerializer.Serialize(rates);
                await _cache.SetStringAsync(_cacheKey, json, options);

                _logger.LogDebug("Successfully cached {Count} exchange rates for {Expiration} minutes",
                    rates.Count, _cacheExpiration.TotalMinutes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cache exchange rates");
                // Don't rethrow - caching errors shouldn't stop the app from working
            }
        }

        private async Task<List<ExchangeRate>?> TryGetFromCacheAsync()
        {
            try
            {
                var ratesJson = await _cache.GetStringAsync(_cacheKey);

                if (string.IsNullOrEmpty(ratesJson))
                {
                    _logger.LogDebug("No exchange rates found in cache");
                    return null;
                }

                var rates = JsonSerializer.Deserialize<List<ExchangeRate>>(ratesJson);

                if (rates is null || rates.Count == 0)
                {
                    _logger.LogWarning("Cached exchange rates were empty or failed to deserialize");
                    return null;
                }

                return rates;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize cached exchange rates");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving exchange rates from cache");
                return null;
            }
        }
    }
}