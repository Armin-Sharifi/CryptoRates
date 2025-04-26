using CryptoRates.UI.API.DataTransferObjects;
using ErrorOr;

namespace CryptoRates.UI.API.Services.Contracts;

public interface ISymbolsService
{
    Task<ErrorOr<List<CryptoSymbol>>> GetSymbolsAsync();
    Task<ErrorOr<List<string>>> ValidateSymbolsAsync(List<string> symbols);
}