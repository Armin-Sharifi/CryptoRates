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
        IConfiguration configuration,
        ILogger<CoinMarketCapService> logger)
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

        _logger.LogInformation("CoinMarketCapService initialized with base URL: {BaseUrl}", _baseUrl);
    }

    public async Task<ErrorOr<List<CryptoSymbol>>> FetchSymbolsAsync()
    {
        _logger.LogInformation("Fetching crypto symbols from CoinMarketCap");

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("X-CMC_PRO_API_KEY", _apiKey);

            var url = $"{_baseUrl.TrimEnd('/')}{_latestListingsEndpoint}";
            _logger.LogDebug("Sending request to: {Url}", url);

            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to fetch symbols from CoinMarketCap. Status code: {StatusCode}, Response: {Response}",
                    response.StatusCode, errorContent);

                return Error.Failure("CoinMarketCap.FetchSymbols.RequestFailed",
                    $"API request failed with status code {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var symbols = ParseSymbolsFromResponse(content);

            _logger.LogInformation("Successfully fetched {Count} crypto symbols", symbols.Count);
            return symbols;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request exception while fetching symbols");
            return Error.Failure("CoinMarketCap.FetchSymbols.RequestException", ex.Message);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing exception while processing symbols response");
            return Error.Failure("CoinMarketCap.FetchSymbols.JsonParsingError", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching symbols");
            return Error.Unexpected("CoinMarketCap.FetchSymbols.UnexpectedError", ex.Message);
        }
    }

    private List<CryptoSymbol> ParseSymbolsFromResponse(string jsonResponse)
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
            _logger.LogError(ex, "Failed to parse CoinMarketCap API response");
            throw new JsonException("Failed to parse CoinMarketCap API response", ex);
        }
    }

    public async Task<ErrorOr<List<QuoteDto>>> GetLatestQuotesAsync(List<string> symbols)
    {
        if (symbols is null || !symbols.Any())
        {
            _logger.LogWarning("GetLatestQuotesAsync called with empty or null symbols list");
            return Error.Validation("CoinMarketCap.GetQuotes.EmptySymbols", "No symbols provided for quotes request");
        }

        _logger.LogInformation("Fetching latest quotes for {Count} symbols: {Symbols}",
            symbols.Count, string.Join(", ", symbols));

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("X-CMC_PRO_API_KEY", _apiKey);

            var url = new UriBuilder($"{_baseUrl.TrimEnd('/')}{_latestQuotesEndpoint}");
            var queryString = HttpUtility.ParseQueryString(string.Empty);
            queryString["symbol"] = string.Join(",", symbols);
            queryString["convert"] = _baseQuotesCurrency;
            url.Query = queryString.ToString();

            _logger.LogDebug("Sending quotes request to: {Url}", url.ToString());

            var response = await client.GetAsync(url.ToString());
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to fetch latest quotes. Status code: {StatusCode}, Response: {Response}",
                    response.StatusCode, errorContent);

                return Error.Failure("CoinMarketCap.GetQuotes.RequestFailed",
                    $"API request failed with status code {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var quotes = ParseQuotesFromResponse(content, symbols);

            if (quotes.Count == 0)
            {
                _logger.LogWarning("No valid quotes returned for symbols: {Symbols}", string.Join(", ", symbols));
                return Error.NotFound("CoinMarketCap.GetQuotes.NoQuotesFound", "No valid quotes were found for the provided symbols");
            }

            _logger.LogInformation("Successfully fetched {Count} quotes", quotes.Count);
            return quotes;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request exception while fetching quotes");
            return Error.Failure("CoinMarketCap.GetQuotes.RequestException", ex.Message);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing exception while processing quotes response");
            return Error.Failure("CoinMarketCap.GetQuotes.JsonParsingError", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching quotes");
            return Error.Unexpected("CoinMarketCap.GetQuotes.UnexpectedError", ex.Message);
        }
    }

    private List<QuoteDto> ParseQuotesFromResponse(string jsonResponse, List<string> symbols)
    {
        var results = new List<QuoteDto>();

        try
        {
            using var document = JsonDocument.Parse(jsonResponse);

            if (!document.RootElement.TryGetProperty("data", out JsonElement dataElement))
            {
                _logger.LogWarning("Missing 'data' property in quotes response");
                return results;
            }

            foreach (var symbol in symbols)
            {
                try
                {
                    if (!dataElement.TryGetProperty(symbol, out JsonElement symbolData))
                    {
                        _logger.LogDebug("Symbol {Symbol} not found in response data", symbol);
                        continue;
                    }

                    var firstItem = symbolData.EnumerateArray().FirstOrDefault();

                    if (firstItem.ValueKind == JsonValueKind.Undefined)
                    {
                        _logger.LogDebug("No data available for symbol {Symbol}", symbol);
                        continue;
                    }

                    if (!firstItem.TryGetProperty("quote", out JsonElement quoteElement))
                    {
                        _logger.LogDebug("Quote property missing for symbol {Symbol}", symbol);
                        continue;
                    }

                    if (!quoteElement.TryGetProperty(_baseQuotesCurrency, out JsonElement currencyElement))
                    {
                        _logger.LogDebug("Currency {Currency} not found in quote for symbol {Symbol}",
                            _baseQuotesCurrency, symbol);
                        continue;
                    }

                    if (!currencyElement.TryGetProperty("price", out JsonElement priceElement))
                    {
                        _logger.LogDebug("Price property missing in quote for symbol {Symbol}", symbol);
                        continue;
                    }

                    var price = priceElement.GetDecimal();
                    results.Add(new QuoteDto(symbol, price));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error processing quote for symbol {Symbol}", symbol);
                    // Continue processing other symbols even if one fails
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse quotes response");
            throw new JsonException("Failed to parse CoinMarketCap quotes response", ex);
        }
    }
}