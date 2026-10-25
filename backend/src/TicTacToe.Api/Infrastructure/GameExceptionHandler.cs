using Microsoft.AspNetCore.Diagnostics;
using TicTacToe.Api.Models;
using TicTacToe.Api.Services;

namespace TicTacToe.Api.Infrastructure;

/// <summary>
/// Turns rule violations into 400 and a missing game into 404, so the controllers stay free of
/// try/catch. Anything else falls through to the default handler as a 500.
/// </summary>
public class GameExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var statusCode = exception switch
        {
            InvalidMoveException => StatusCodes.Status400BadRequest,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            _ => 0
        };

        if (statusCode == 0)
        {
            return false;
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(new ErrorResponse(exception.Message), cancellationToken);
        return true;
    }
}
