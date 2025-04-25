namespace CryptoRates.UI.API.Extensions;

public static class MiddleWareExtension
{
    public static WebApplication ConfigureMiddlewares(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI();

        return app;
    }
}