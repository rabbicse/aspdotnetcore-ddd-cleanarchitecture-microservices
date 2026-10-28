using Mehedi.Application.SharedKernel.Enums;
using Mehedi.Application.SharedKernel.Responses;
using Microsoft.AspNetCore.Mvc;

namespace KYC.API.Http;

/// <summary>Maps application <see cref="Result{T}"/> values onto HTTP responses.</summary>
internal static class ResultExtensions
{
    public static IActionResult ToActionResult<T>(
        this Result<T> result,
        ControllerBase controller,
        Func<T, IActionResult>? onSuccess = null)
    {
        if (result.Status == ResultStatus.Ok)
        {
            return onSuccess is null ? controller.Ok(result.Value) : onSuccess(result.Value);
        }

        var (statusCode, title) = result.Status switch
        {
            ResultStatus.NotFound => (StatusCodes.Status404NotFound, "Resource not found."),
            ResultStatus.Invalid => (StatusCodes.Status400BadRequest, "The request is invalid."),
            ResultStatus.Conflict => (StatusCodes.Status409Conflict, "The request conflicts with the current state."),
            ResultStatus.Unauthorized => (StatusCodes.Status401Unauthorized, "Authentication is required."),
            ResultStatus.Forbidden => (StatusCodes.Status403Forbidden, "You are not allowed to do this."),
            ResultStatus.Unavailable => (StatusCodes.Status503ServiceUnavailable, "The service is temporarily unavailable."),
            _ => (StatusCodes.Status500InternalServerError, "The request could not be completed.")
        };

        var detail = string.Join(" ", result.Errors);

        return controller.Problem(
            detail: detail.Length == 0 ? null : detail,
            statusCode: statusCode,
            title: title);
    }
}
