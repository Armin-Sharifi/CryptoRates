using CryptoRates.UI.API.DataTransferObjects;

namespace CryptoRates.UI.API.ExternalServices.Contracts;

public interface ICoinMarketCapService
{
    Task<List<SymbolDto>> FetchSymbolsAsync();
}