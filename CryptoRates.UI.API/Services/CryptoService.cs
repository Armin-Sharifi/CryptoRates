using CryptoRates.UI.API.ExternalServices.Contracts;
using Microsoft.Extensions.Caching.Distributed;

namespace CryptoRates.UI.API.Services;

public class CryptoService
{
    private readonly IDistributedCache _cache;
    private readonly ICoinMarketCapService _coinMarketCapService;
    private readonly string _cacheKey;
    private readonly TimeSpan _cacheExpiration;

    public CryptoService(IDistributedCache cache, ICoinMarketCapService coinMarketCapService, IConfiguration configuration)
    {
        _cache = cache;
        _coinMarketCapService = coinMarketCapService;
        _cacheKey = configuration["CryptoCache:CryptoCacheKey"] ?? "crypto-cache";
        _cacheExpiration = TimeSpan.FromMinutes(
            configuration.GetValue<double>("CryptoCache:ExpirationMinutes", 1)); // Default 1 minute
    }
}