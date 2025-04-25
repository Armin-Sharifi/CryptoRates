using CryptoRates.UI.API.DataTransferObjects;

namespace CryptoRates.UI.API.Services.Contracts;

public interface ISymbolsRepository
{
    Task<List<SymbolDto>> GetSymbolsAsync();
    Task<bool> IsSymbolExistAsync(string symbol);
}