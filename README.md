# CryptoRates

CryptoRates is a .NET-based API project for retrieving cryptocurrency exchange rates and supported symbols.

## Setup

To run the project, you need to initialize API keys using .NET user secrets. Execute the following commands to set up the required API keys:

```bash
dotnet user-secrets set "ExchangeRates:ApiKey" "Your-API-Key" --project CryptoRates.UI.API.csproj
dotnet user-secrets set "CoinMarketCap:ApiKey" "Your-API-Key" --project CryptoRates.UI.API.csproj
```

Replace `Your-API-Key` with the actual API keys obtained from the respective services.

## API Endpoints

### Get Quotes
- **Endpoint**: `/api/quotes`
- **Description**: Retrieves the latest price quotes for one or more cryptocurrency symbols in multiple currencies (USD, EUR, BRL, GBP, AUD).
- **Query Parameters**:
  - `symbols`: A single symbol or multiple symbols (comma-separated). Example: `BTC,ETH`.
- **Example**:
  ```
  GET /api/quotes?symbols=BTC,ETH
  ```
  Returns prices for BTC and ETH in USD, EUR, BRL, GBP, and AUD.

### Get Available Symbols
- **Endpoint**: `/api/symbols`
- **Description**: Lists all available cryptocurrency symbols supported by the API.
- **Example**:
  ```
  GET /api/symbols
  ```
  Returns a list of supported symbols that can be used in the `/api/quotes` endpoint.

## Notes
- Ensure the API keys are correctly configured in the user secrets to avoid authentication errors.
- For a full list of supported symbols, use the `/api/symbols` endpoint before querying `/api/quotes`.
