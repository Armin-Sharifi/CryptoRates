using System.Text.Json;
using CryptoRates.UI.API.DataTransferObjects;
using CryptoRates.UI.API.ExternalServices.Contracts;

namespace CryptoRates.UI.API.ExternalServices;

public class CoinMarketCapService : ICoinMarketCapService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly string _latestListingsEndpoint;

    public CoinMarketCapService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _apiKey = configuration["CoinMarketCap:ApiKey"]
            ?? throw new InvalidOperationException("CoinMarketCap API key is not configured");
        _baseUrl = configuration["CoinMarketCap:BaseUrl"]
            ?? "https://pro-api.coinmarketcap.com";
        _latestListingsEndpoint = configuration["CoinMarketCap:LatestListingsEndpoint"]
            ?? "/v1/cryptocurrency/listings/latest";
    }

    public async Task<List<SymbolDto>> FetchSymbolsAsync()
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("X-CMC_PRO_API_KEY", _apiKey);

        var url = $"{_baseUrl.TrimEnd('/')}{_latestListingsEndpoint}";
        var response = await client.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Failed to fetch symbols from CoinMarketCap. Status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        return ParseSymbolsFromResponse(content);
    }

    private static List<SymbolDto> ParseSymbolsFromResponse(string jsonResponse)
    {
        try
        {
            var json = JsonDocument.Parse(jsonResponse);
            var symbols = json.RootElement.GetProperty("data")
                .EnumerateArray()
                .Select(x => new SymbolDto(x.GetProperty("symbol").GetString(), x.GetProperty("name").GetString()))
                .Where(x => !string.IsNullOrWhiteSpace(x.Name) && !string.IsNullOrWhiteSpace(x.Symbol))
                .DistinctBy(x => x.Symbol)
                .ToList();

            return symbols;
        }
        catch (Exception ex)
        {
            throw new JsonException("Failed to parse CoinMarketCap API response", ex);
        }
    }
}