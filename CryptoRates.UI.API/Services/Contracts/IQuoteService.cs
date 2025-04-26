using CryptoRates.UI.API.DataTransferObjects;
using ErrorOr;

namespace CryptoRates.UI.API.Services.Contracts;

public interface IQuoteService
{
    Task<ErrorOr<List<QuoteResult>>> GetPricesAsync(List<string> symbols);
}