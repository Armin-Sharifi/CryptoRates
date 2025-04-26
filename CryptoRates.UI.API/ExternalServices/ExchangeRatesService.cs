using CryptoRates.UI.API.DataTransferObjects;
using CryptoRates.UI.API.ExternalServices.Contracts;
using System.Text.Json;
using System.Web;
using ErrorOr;

namespace CryptoRates.UI.API.ExternalServices;

public class ExchangeRatesService : IExchangeRatesService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ExchangeRatesService> _logger;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly string _latestExchangeEndpoint;
    private readonly string _baseCurrency;
    private readonly string _exchangeCurrencies;

    public ExchangeRatesService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<ExchangeRatesService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;

        _apiKey = configuration["ExchangeRates:ApiKey"] ?? throw new InvalidOperationException("ExchangeRates API key is not configured");

        _baseUrl = configuration["ExchangeRates:BaseUrl"] ?? "https://api.exchangeratesapi.io";

        _latestExchangeEndpoint = configuration["ExchangeRates:LatestExchangeEndpoint"] ?? "/v2/cryptocurrency/quotes/latest";

        _baseCurrency = configuration["ExchangeRates:BaseCurrency"] ?? "EUR";

        _exchangeCurrencies = configuration["ExchangeRates:ExchangeCurrencies"] ?? "USD,BRL,GBP,AUD";

        _logger.LogInformation("ExchangeRatesService initialized with base URL: {BaseUrl} and base currency: {BaseCurrency}",
            _baseUrl, _baseCurrency);
    }

    public async Task<ErrorOr<List<ExchangeRate>>> GetRates()
    {
        _logger.LogInformation("Fetching exchange rates with base currency: {BaseCurrency}", _baseCurrency);

        try
        {
            var client = _httpClientFactory.CreateClient();

            var url = new UriBuilder($"{_baseUrl.TrimEnd('/')}{_latestExchangeEndpoint}");
            var queryString = HttpUtility.ParseQueryString(string.Empty);
            queryString["access_key"] = _apiKey;
            queryString["base"] = _baseCurrency;
            queryString["symbols"] = _exchangeCurrencies;
            url.Query = queryString.ToString();

            _logger.LogDebug("Sending exchange rates request to: {Url}", url.ToString());

            var response = await client.GetAsync(url.ToString());

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to fetch exchange rates. Status code: {StatusCode}, Response: {Response}",
                    response.StatusCode, errorContent);

                return Error.Failure("ExchangeRates.GetRates.RequestFailed",
                    $"API request failed with status code {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var rates = ParseRatesFromResponse(content);

            if (rates.Count == 0)
            {
                _logger.LogWarning("No exchange rates found in the API response");
                return Error.NotFound("ExchangeRates.GetRates.NoRatesFound",
                    "No exchange rates were found in the API response");
            }

            _logger.LogInformation("Successfully fetched {Count} exchange rates", rates.Count);
            return rates;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request exception while fetching exchange rates");
            return Error.Failure("ExchangeRates.GetRates.RequestException", ex.Message);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing exception while processing exchange rates response");
            return Error.Failure("ExchangeRates.GetRates.JsonParsingError", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching exchange rates");
            return Error.Unexpected("ExchangeRates.GetRates.UnexpectedError", ex.Message);
        }
    }

    private List<ExchangeRate> ParseRatesFromResponse(string responseJson)
    {
        var results = new List<ExchangeRate>();

        try
        {
            using var document = JsonDocument.Parse(responseJson);
            var rootElement = document.RootElement;

            if (!rootElement.TryGetProperty("rates", out var ratesElement))
            {
                _logger.LogWarning("Missing 'rates' property in exchange rates response");
                return results;
            }

            foreach (var rate in ratesElement.EnumerateObject())
            {
                try
                {
                    var currencyCode = rate.Name;

                    if (rate.Value.ValueKind != JsonValueKind.Number)
                    {
                        _logger.LogWarning("Invalid rate value for currency {CurrencyCode}", currencyCode);
                        continue;
                    }

                    var rateValue = rate.Value.GetDecimal();
                    results.Add(new ExchangeRate(currencyCode, rateValue));
                    _logger.LogDebug("Parsed exchange rate: {CurrencyCode} = {RateValue}", currencyCode, rateValue);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error processing exchange rate for currency {CurrencyCode}", rate.Name);
                    // Continue processing other rates even if one fails
                }
            }

            if (results.Count == 0)
            {
                _logger.LogWarning("No valid exchange rates found in response");
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse exchange rates response");
            throw new JsonException("Failed to parse ExchangeRates API response", ex);
        }
    }
}