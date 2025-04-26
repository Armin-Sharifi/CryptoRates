using CryptoRates.UI.API.DataTransferObjects;
using ErrorOr;

namespace CryptoRates.UI.API.Services.Contracts;

public interface IRateService
{
    Task<ErrorOr<List<ExchangeRate>>> GetRatesAsync();
}