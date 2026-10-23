using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OnlyClub.Core.Common;

namespace OnlyClub.API.Middleware;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Произошло исключение: {Message}", exception.Message);

        var problemDetails = new ProblemDetails
        {
            Instance = httpContext.Request.Path
        };

        if (exception is DomainException domainEx)
        {
            problemDetails.Status = StatusCodes.Status400BadRequest;
            problemDetails.Title = "Ошибка бизнес-логики";
            problemDetails.Detail = domainEx.Message;       
        }
        else if (exception is NotFoundException notFoundEx)
        {
            problemDetails.Status = StatusCodes.Status404NotFound;
            problemDetails.Title = "Не найдено";
            problemDetails.Detail = notFoundEx.Message;
        }
        else if (exception is ForbiddenException forbiddenEx)
        {
            problemDetails.Status = StatusCodes.Status403Forbidden;
            problemDetails.Title = "Доступ запрещен";
            problemDetails.Detail = forbiddenEx.Message;
        }
        else
        {
            problemDetails.Status = StatusCodes.Status500InternalServerError;
            problemDetails.Title = "Внутренняя ошибка сервера";
            problemDetails.Detail = "Что-то пошло не так. Мы уже разбираемся.";
        }

        httpContext.Response.StatusCode = problemDetails.Status.Value;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;       
    }
}