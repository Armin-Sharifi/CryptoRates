using CryptoRates.UI.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationServices();

var app = builder.Build();

app.ConfigureMiddlewares();

app.Run();