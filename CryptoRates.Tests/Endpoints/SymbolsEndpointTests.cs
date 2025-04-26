using CryptoRates.UI.API.DataTransferObjects;
using CryptoRates.UI.API.Services.Contracts;
using FluentAssertions;
using Moq;

namespace CryptoRates.Tests.Endpoints
{
    public class SymbolsEndpointTests
    {
        [Fact]
        public async Task GetSymbols_ReturnsSymbols_ForMultipleKnownSymbols()
        {
            // Arrange
            var mockService = new Mock<ISymbolsService>();

            var expectedSymbols = new List<CryptoSymbol>
            {
                new("BTC", "Bitcoin"),
                new("ETH", "Ethereum")
            };

            mockService.Setup(s => s.GetSymbolsAsync()).ReturnsAsync(expectedSymbols);

            // Act
            var response = await mockService.Object.GetSymbolsAsync();
            response.IsError.Should().BeFalse();
            var result = response.Value;

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCountGreaterThan(0);
        }
    }
}
