using CryptoRates.UI.API.DataTransferObjects;
using CryptoRates.UI.API.Services.Contracts;
using ErrorOr;
using FluentAssertions;
using Moq;

namespace CryptoRates.Tests.Endpoints;

public class QuotesEndpointTests
{
    [Fact]
    public async Task GetQuotes_ReturnsQuotes_ForKnownSymbol()
    {
        // Arrange
        var mockService = new Mock<IQuoteService>();
        var symbols = "BTC";

        var expectedResult = new List<QuoteResult>
        {
            new(
                new CryptoSymbol("Bitcoin", "BTC"),
                [
                    new Price("USD", 94336.68m),
                    new Price("EUR", 82743.06m),
                    new Price("BRL", 536813.65m),
                    new Price("GBP", 70860.57m),
                    new Price("AUD", 147193.86m)
                ]
            )
        };

        mockService
            .Setup(s => s.GetPricesAsync(It.Is<List<string>>(list => list.Contains("BTC"))))
            .ReturnsAsync(expectedResult);

        // Act
        var response = await mockService.Object.GetPricesAsync([symbols]);

        // Assert
        response.IsError.Should().BeFalse();
        var result = response.Value;

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].CryptoSymbol.Symbol.Should().Be("BTC");
        result[0].Prices.Should().Contain(p => p.Icon == "USD");
        result[0].Prices.Should().Contain(p => p.Icon == "EUR");
        result[0].Prices.Should().Contain(p => p.Icon == "BRL");
        result[0].Prices.Should().Contain(p => p.Icon == "GBP");
        result[0].Prices.Should().Contain(p => p.Icon == "AUD");
    }

    [Fact]
    public async Task GetQuotes_ReturnsQuotes_ForMultipleKnownSymbols()
    {
        // Arrange
        var mockService = new Mock<IQuoteService>();
        var symbols = "BTC,ETH";

        var expectedResult = new List<QuoteResult>
        {
            new(
                new CryptoSymbol("Bitcoin", "BTC"),
                [
                    new Price("USD", 94336.68m),
                    new Price("EUR", 82743.06m),
                    new Price("BRL", 536813.65m),
                    new Price("GBP", 70860.57m),
                    new Price("AUD", 147193.86m)
                ]
            ),
            new(
                new CryptoSymbol("Ethereum", "ETH"),
                [
                    new Price("USD", 1802.67m),
                    new Price("EUR", 1581.13m),
                    new Price("BRL", 10257.96m),
                    new Price("GBP", 1354.07m),
                    new Price("AUD", 2812.72m)
                ]
            )
        };

        mockService.Setup(s => s.GetPricesAsync(It.Is<List<string>>(list => list.Contains("BTC") && list.Contains("ETH")))).ReturnsAsync(ErrorOrFactory.From(expectedResult));

        // Act
        var response = await mockService.Object.GetPricesAsync(["BTC", "ETH"]);

        // Assert
        response.IsError.Should().BeFalse();
        var result = response.Value;

        result.Should().NotBeNull();
        result.Should().HaveCount(2);

        result.Should().Contain(r => r.CryptoSymbol.Symbol == "BTC");
        result.Should().Contain(r => r.CryptoSymbol.Symbol == "ETH");

        var btc = result.First(r => r.CryptoSymbol.Symbol == "BTC");
        btc.Prices.Should().Contain(p => p.Icon == "USD");
        btc.Prices.Should().Contain(p => p.Icon == "EUR");
        btc.Prices.Should().Contain(p => p.Icon == "BRL");
        btc.Prices.Should().Contain(p => p.Icon == "GBP");
        btc.Prices.Should().Contain(p => p.Icon == "AUD");

        var eth = result.First(r => r.CryptoSymbol.Symbol == "ETH");
        eth.Prices.Should().Contain(p => p.Icon == "USD");
        eth.Prices.Should().Contain(p => p.Icon == "EUR");
        eth.Prices.Should().Contain(p => p.Icon == "BRL");
        eth.Prices.Should().Contain(p => p.Icon == "GBP");
        eth.Prices.Should().Contain(p => p.Icon == "AUD");
    }

    [Fact]
    public async Task GetQuotes_ReturnsError_ForUnknownSymbol()
    {
        var mockService = new Mock<IQuoteService>();
        var symbols = "UNKNOWN";

        var error = Error.Validation(
            code: "QuoteService.ValidateSymbols.Error",
            description: "The symbols you provided are not valid."
        );

        mockService.Setup(s => s.GetPricesAsync(It.Is<List<string>>(list => list.Contains("UNKNOWN")))).ReturnsAsync(error);

        // Act
        var response = await mockService.Object.GetPricesAsync([symbols]);

        // Assert
        response.IsError.Should().BeTrue();
        response.FirstError.Code.Should().Be("QuoteService.ValidateSymbols.Error");
        response.FirstError.Description.Should().Be("The symbols you provided are not valid.");
    }
}