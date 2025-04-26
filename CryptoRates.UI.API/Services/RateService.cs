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
        private readonly string _cacheKey;
        private readonly TimeSpan _cacheExpiration;

        public RateService(IDistributedCache cache, IExchangeRatesService exchangeRatesService, IConfiguration configuration)
        {
            _cache = cache;
            _exchangeRatesService = exchangeRatesService;
            _cacheKey = configuration["CacheRates:CryptoSymbolsKey"] ?? "exchange-rates";
            _cacheExpiration = TimeSpan.FromMinutes(
                configuration.GetValue<double>("CacheRates:ExpirationMinutes", 60)); // Default 1 hour
        }

        public async Task<ErrorOr<List<ExchangeRate>>> GetRatesAsync()
        {
            var cachedRates = await TryGetFromCacheAsync();
            if (cachedRates is not null && cachedRates.Count > 0)
                return cachedRates;

            var rates = await _exchangeRatesService.GetRates();

            if (rates.IsError)
            {
                return rates.Errors;
            }

            await SetToCacheAsync(rates.Value);
            return rates;
        }

        private async Task SetToCacheAsync(List<ExchangeRate> rates)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cacheExpiration
            };
            var json = JsonSerializer.Serialize(rates);
            await _cache.SetStringAsync(_cacheKey, json, options);
        }

        private async Task<List<ExchangeRate>?> TryGetFromCacheAsync()
        {
            var ratesJson = await _cache.GetStringAsync(_cacheKey);
            return string.IsNullOrEmpty(ratesJson)
                ? null
                : JsonSerializer.Deserialize<List<ExchangeRate>>(ratesJson);
        }
    }
}
