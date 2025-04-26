using System.Net;
using CryptoRates.UI.API.ExternalServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace CryptoRates.Tests.ExternalServices;

public class CoinMarketCapServiceTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<CoinMarketCapService>> _loggerMock;
    private readonly IConfiguration _configuration;

    public CoinMarketCapServiceTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<CoinMarketCapService>>();

        var inMemorySettings = new Dictionary<string, string> {
            {"CoinMarketCap:ApiKey", "test-api-key"},
            {"CoinMarketCap:BaseUrl", "https://fake-url.com"},
            {"CoinMarketCap:LatestListingsEndpoint", "/v1/cryptocurrency/listings/latest"},
            {"CoinMarketCap:LatestQuotesEndpoint", "/v2/cryptocurrency/quotes/latest"},
            {"CoinMarketCap:BaseQuotesCurrency", "EUR"}
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }

    private HttpClient CreateHttpClient(HttpResponseMessage responseMessage)
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        return new HttpClient(handlerMock.Object);
    }

    [Fact]
    public async Task FetchSymbolsAsync_ShouldReturnSymbols_WhenApiResponseIsSuccessful()
    {
        // Arrange
        var jsonResponse = @"{
            ""data"": [
                { ""name"": ""Bitcoin"", ""symbol"": ""BTC"" },
                { ""name"": ""Ethereum"", ""symbol"": ""ETH"" }
            ]
        }";

        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse)
        };

        var client = CreateHttpClient(responseMessage);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);

        var service = new CoinMarketCapService(_httpClientFactoryMock.Object, _configuration, _loggerMock.Object);

        // Act
        var result = await service.FetchSymbolsAsync();

        // Assert
        Assert.False(result.IsError);
        Assert.Equal(2, result.Value.Count);
        Assert.Contains(result.Value, x => x.Symbol == "BTC");
        Assert.Contains(result.Value, x => x.Symbol == "ETH");
    }

    [Fact]
    public async Task FetchSymbolsAsync_ShouldReturnError_WhenApiFails()
    {
        // Arrange
        var responseMessage = new HttpResponseMessage(HttpStatusCode.InternalServerError);

        var client = CreateHttpClient(responseMessage);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);

        var service = new CoinMarketCapService(_httpClientFactoryMock.Object, _configuration, _loggerMock.Object);

        // Act
        var result = await service.FetchSymbolsAsync();

        // Assert
        Assert.True(result.IsError);
        Assert.Equal("CoinMarketCap.FetchSymbols.RequestFailed", result.FirstError.Code);
    }

    [Fact]
    public async Task GetLatestQuotesAsync_ShouldReturnError_WhenSymbolsListIsEmpty()
    {
        // Arrange
        var service = new CoinMarketCapService(_httpClientFactoryMock.Object, _configuration, _loggerMock.Object);

        // Act
        var result = await service.GetLatestQuotesAsync(new List<string>());

        // Assert
        Assert.True(result.IsError);
        Assert.Equal("CoinMarketCap.GetQuotes.EmptySymbols", result.FirstError.Code);
    }

    [Fact]
    public async Task GetLatestQuotesAsync_ShouldReturnQuotes_WhenApiResponseIsSuccessful()
    {
        // Arrange
        var jsonResponse = @"{
            ""data"": {
                ""BTC"": [
                    {
                        ""quote"": {
                            ""EUR"": {
                                ""price"": 25000.0
                            }
                        }
                    }
                ]
            }
        }";

        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse)
        };

        var client = CreateHttpClient(responseMessage);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);

        var service = new CoinMarketCapService(_httpClientFactoryMock.Object, _configuration, _loggerMock.Object);

        // Act
        var result = await service.GetLatestQuotesAsync(new List<string> { "BTC" });

        // Assert
        Assert.False(result.IsError);
        Assert.Single(result.Value);
        Assert.Equal("BTC", result.Value[0].Symbol);
        Assert.Equal(25000.0m, result.Value[0].Price);
    }
}