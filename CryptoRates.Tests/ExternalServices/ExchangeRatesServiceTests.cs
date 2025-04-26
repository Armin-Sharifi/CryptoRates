using CryptoRates.UI.API.ExternalServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq.Protected;
using Moq;
using System.Net;
using System.Text.Json;
using Xunit;

namespace CryptoRates.Tests.ExternalServices
{
    public class ExchangeRatesServiceTests
    {
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly Mock<ILogger<ExchangeRatesService>> _loggerMock;
        private readonly IConfiguration _configuration;

        public ExchangeRatesServiceTests()
        {
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _loggerMock = new Mock<ILogger<ExchangeRatesService>>();

            var inMemorySettings = new Dictionary<string, string>
        {
            {"ExchangeRates:ApiKey", "test-api-key"},
            {"ExchangeRates:BaseUrl", "https://api.exchangeratesapi.io"},
            {"ExchangeRates:LatestExchangeEndpoint", "/latest"},
            {"ExchangeRates:BaseCurrency", "EUR"},
            {"ExchangeRates:ExchangeCurrencies", "USD,GBP"}
        };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }

        [Fact]
        public async Task GetRates_ShouldReturnRates_WhenApiResponseIsSuccessful()
        {
            // Arrange
            var responseContent = JsonSerializer.Serialize(new
            {
                rates = new
                {
                    USD = 1.1,
                    GBP = 0.9
                }
            });

            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseContent)
                });

            var client = new HttpClient(handlerMock.Object);

            _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

            var service = new ExchangeRatesService(_httpClientFactoryMock.Object, _configuration, _loggerMock.Object);

            // Act
            var result = await service.GetRates();

            // Assert
            Assert.False(result.IsError);
            Assert.Equal(2, result.Value.Count);

            Assert.Contains(result.Value, r => r is { Icon: "USD", Rate: > 0 });
            Assert.Contains(result.Value, r => r is { Icon: "GBP", Rate: > 0 });
        }

        [Fact]
        public async Task GetRates_ShouldReturnError_WhenApiResponseFails()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("{\"error\":\"Bad Request\"}")
                });

            var client = new HttpClient(handlerMock.Object);

            _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

            var service = new ExchangeRatesService(_httpClientFactoryMock.Object, _configuration, _loggerMock.Object);

            // Act
            var result = await service.GetRates();

            // Assert
            Assert.True(result.IsError);
            Assert.Equal("ExchangeRates.GetRates.RequestFailed", result.FirstError.Code);
        }

        [Fact]
        public async Task GetRates_ShouldReturnError_WhenResponseIsInvalidJson()
        {
            // Arrange
            var invalidJson = "{invalid json}";

            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(invalidJson)
                });

            var client = new HttpClient(handlerMock.Object);

            _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

            var service = new ExchangeRatesService(_httpClientFactoryMock.Object, _configuration, _loggerMock.Object);

            // Act
            var result = await service.GetRates();

            // Assert
            Assert.True(result.IsError);
            Assert.Equal("ExchangeRates.GetRates.JsonParsingError", result.FirstError.Code);
        }
    }
}
