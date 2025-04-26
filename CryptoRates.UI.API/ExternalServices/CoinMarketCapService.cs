using System;
using System.Text.Json;
using System.Web;
using CryptoRates.UI.API.DataTransferObjects;
using CryptoRates.UI.API.ExternalServices.Contracts;
using ErrorOr;

namespace CryptoRates.UI.API.ExternalServices;

public class CoinMarketCapService : ICoinMarketCapService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CoinMarketCapService> _logger;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly string _latestListingsEndpoint;
    private readonly string _latestQuotesEndpoint;
    private readonly string _baseQuotesCurrency;

    public CoinMarketCapService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration, ILogger<CoinMarketCapService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;

        _apiKey = configuration["CoinMarketCap:ApiKey"]
                  ?? throw new InvalidOperationException("CoinMarketCap API key is not configured");

        _baseUrl = configuration["CoinMarketCap:BaseUrl"]
            ?? "https://pro-api.coinmarketcap.com";

        _latestListingsEndpoint = configuration["CoinMarketCap:LatestListingsEndpoint"]
            ?? "/v1/cryptocurrency/listings/latest";

        _latestQuotesEndpoint = configuration["CoinMarketCap:LatestQuotesEndpoint"]
                                  ?? "/v2/cryptocurrency/quotes/latest";

        _baseQuotesCurrency = configuration["CoinMarketCap:BaseQuotesCurrency"] ?? "EUR";
    }

    public async Task<ErrorOr<List<CryptoSymbol>>> FetchSymbolsAsync()
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

    private static List<CryptoSymbol> ParseSymbolsFromResponse(string jsonResponse)
    {
        try
        {
            var json = JsonDocument.Parse(jsonResponse);
            var symbols = json.RootElement.GetProperty("data")
                .EnumerateArray()
                .Select(x => new CryptoSymbol(x.GetProperty("name").GetString(), x.GetProperty("symbol").GetString()))
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

    public async Task<ErrorOr<List<QuoteDto>>> GetLatestQuotesAsync(List<string> symbols)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("X-CMC_PRO_API_KEY", _apiKey);

        var url = new UriBuilder($"{_baseUrl.TrimEnd('/')}{_latestQuotesEndpoint}");
        var queryString = HttpUtility.ParseQueryString(string.Empty);
        queryString["symbol"] = string.Join(",", symbols);
        queryString["convert"] = _baseQuotesCurrency;
        url.Query = queryString.ToString();

        var response = await client.GetAsync(url.ToString());
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Failed to fetch latest quotes from CoinMarketCap. Status code: {response.StatusCode}");
        }
        var content = await response.Content.ReadAsStringAsync();
        return ParseQuotesFromResponse(content, symbols);
    }

    private List<QuoteDto> ParseQuotesFromResponse(string jsonResponse, List<string> symbols)
    {
        var results = new List<QuoteDto>();

        using var document = JsonDocument.Parse(jsonResponse);

        var dataElement = document.RootElement.GetProperty("data");

        foreach (var symbol in symbols)
        {
            if (!dataElement.TryGetProperty(symbol, out JsonElement symbolData)) continue;
            var firstItem = symbolData.EnumerateArray().FirstOrDefault();

            if (firstItem.ValueKind == JsonValueKind.Undefined) continue;

            var price = firstItem.GetProperty("quote")
                .GetProperty(_baseQuotesCurrency)
                .GetProperty("price")
                .GetDecimal();

            results.Add(new QuoteDto(symbol, price));
        }

        return results;
    }
}