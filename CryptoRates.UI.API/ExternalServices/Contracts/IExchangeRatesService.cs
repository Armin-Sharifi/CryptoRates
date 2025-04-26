using CryptoRates.UI.API.DataTransferObjects;
using ErrorOr;

namespace CryptoRates.UI.API.ExternalServices.Contracts;

public interface IExchangeRatesService
{
    Task<ErrorOr<List<ExchangeRate>>> GetRates();
}