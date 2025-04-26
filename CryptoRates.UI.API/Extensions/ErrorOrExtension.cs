﻿using ErrorOr;

namespace CryptoRates.UI.API.Extensions;

public static class ErrorOrExtensions
{
    public static IResult ToApiResult<T>(this ErrorOr<T> result)
    {
        if (result.IsError)
        {
            var firstError = result.FirstError;

            var statusCode = firstError.Type switch
            {
                ErrorType.Validation => StatusCodes.Status400BadRequest,
                ErrorType.NotFound => StatusCodes.Status404NotFound,
                ErrorType.Conflict => StatusCodes.Status409Conflict,
                ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
                ErrorType.Forbidden => StatusCodes.Status403Forbidden,
                ErrorType.Failure => StatusCodes.Status500InternalServerError,
                _ => StatusCodes.Status500InternalServerError, // fallback
            };

            return Results.Problem(
                detail: string.Join(", ", result.Errors.Select(e => e.Description)),
                statusCode: statusCode,
                title: firstError.Code
            );
        }

        return Results.Ok(result.Value);
    }
}