using CryptoRates.UI.API.DataTransferObjects;
using CryptoRates.UI.API.ExternalServices.Contracts;
using System.Text.Json;
using System.Web;
using ErrorOr;

namespace CryptoRates.UI.API.ExternalServices;

public class ExchangeRatesService : IExchangeRatesService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly string _latestExchangeEndpoint;
    private readonly string _baseCurrency;
    private readonly string _exchangeCurrencies;

    public ExchangeRatesService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;

        _apiKey = configuration["ExchangeRates:ApiKey"] ?? throw new InvalidOperationException("ExchangeRates API key is not configured");

        _baseUrl = configuration["ExchangeRates:BaseUrl"] ?? "https://api.exchangeratesapi.io";

        _latestExchangeEndpoint = configuration["ExchangeRates:LatestExchangeEndpoint"] ?? "/v2/cryptocurrency/quotes/latest";

        _baseCurrency = configuration["ExchangeRates:BaseCurrency"] ?? "EUR";
           
        _exchangeCurrencies = configuration["ExchangeRates:ExchangeCurrencies"] ?? "USD,BRL,GBP,AUD";
    }

    public async Task<ErrorOr<List<ExchangeRate>>> GetRates()
    {
        var client = _httpClientFactory.CreateClient();

        var url = new UriBuilder($"{_baseUrl.TrimEnd('/')}{_latestExchangeEndpoint}");

        var queryString = HttpUtility.ParseQueryString(string.Empty);
        queryString["access_key"] = _apiKey;
        queryString["base"] = _baseCurrency;
        queryString["symbols"] = _exchangeCurrencies;
        url.Query = queryString.ToString();

        var response = await client.GetAsync(url.ToString());
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Failed to fetch latest rates from ExchangeRates . Status code: {response.StatusCode}");
        }
        var content = await response.Content.ReadAsStringAsync();
        return ParseRatesFromResponse(content);
    }

    private List<ExchangeRate> ParseRatesFromResponse(string responseJson)
    {
        var results = new List<ExchangeRate>();
        using var document = JsonDocument.Parse(responseJson);
        var rootElement = document.RootElement;

        if (rootElement.TryGetProperty("rates", out var ratesElement))
        {
            foreach (var rate in ratesElement.EnumerateObject())
            {
                var currencyCode = rate.Name;
                var rateValue = rate.Value.GetDecimal();

                results.Add(new ExchangeRate(currencyCode, rateValue));
            }
        }

        return results;
    }
}