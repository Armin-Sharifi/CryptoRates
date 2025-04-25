using CryptoRates.UI.API.Endpoints;
using CryptoRates.UI.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationServices();

var app = builder.Build();

app.AddSymbolsEndpoints();

app.ConfigureMiddlewares();

app.Run();