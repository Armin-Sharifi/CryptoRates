namespace CryptoRates.UI.API.DataTransferObjects;

public record QuoteResult(CryptoSymbol CryptoSymbol, List<Price> Prices);