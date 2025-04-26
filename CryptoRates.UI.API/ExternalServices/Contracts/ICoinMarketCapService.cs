using CryptoRates.UI.API.DataTransferObjects;
using ErrorOr;

namespace CryptoRates.UI.API.ExternalServices.Contracts;

public interface ICoinMarketCapService
{
    Task<ErrorOr<List<CryptoSymbol>>> FetchSymbolsAsync();
    Task<ErrorOr<List<QuoteDto>>> GetLatestQuotesAsync(List<string> symbols);
}