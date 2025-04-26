using CryptoRates.UI.API.DataTransferObjects;

namespace CryptoRates.UI.API.Services.Contracts;

public interface ISymbolsService
{
    Task<List<SymbolDto>> GetSymbolsAsync();
    Task<List<string>> ValidateSymbolsAsync(List<string> symbols);
}