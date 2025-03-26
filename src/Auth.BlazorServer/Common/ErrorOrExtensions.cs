using ErrorOr;
using Microsoft.AspNetCore.Mvc;

namespace Auth.BlazorServer.Common
{
    public static class ErrorOrExtensions
    {
        // Minimal API için mevcut metotlar
        public static IResult ToMinimalApiResult<T>(this ErrorOr<T> errorOr)
        {
            return errorOr.Match(
                success => Results.Ok(success),
                errors => Results.Problem(CreateProblemDetails(errors))
            );
        }

        public static IResult ToMinimalApiResult(this ErrorOr<Success> errorOr)
        {
            return errorOr.Match(
               _ => Results.Ok(),
               errors => Results.Problem(CreateProblemDetails(errors))
           );
        }

        public static ProblemDetails ToProblemDetails<T>(this ErrorOr<T> errorOr)
        {
            return errorOr.Match(
                success => new ProblemDetails
                {
                    Title = "Operation succeeded",
                    Status = StatusCodes.Status200OK,
                    Detail = "The operation completed successfully."
                },
                errors => CreateProblemDetails(errors)
            );
        }

        public static ProblemDetails ToProblemDetails(this ErrorOr<Success> errorOr)
        {
            return errorOr.Match(
                _ => new ProblemDetails
                {
                    Title = "Operation succeeded",
                    Status = StatusCodes.Status200OK,
                    Detail = "The operation completed successfully."
                },
                errors => CreateProblemDetails(errors)
            );
        }

        // Yeni eklenen metotlar: Controller'larda ActionResult olarak kullanabilmek için

        public static ActionResult<T> ToActionResult<T>(this ErrorOr<T> errorOr)
        {
            return errorOr.Match(
                success => new OkObjectResult(success),
                errors =>
                {
                    var problemDetails = CreateProblemDetails(errors);
                    return new ObjectResult(problemDetails)
                    {
                        StatusCode = problemDetails.Status
                    };
                }
            );
        }

        public static ActionResult ToActionResult(this ErrorOr<Success> errorOr)
        {
            return errorOr.Match<ActionResult>(
                _ => new OkResult(),
                errors =>
                {
                    var problemDetails = CreateProblemDetails(errors);
                    return new ObjectResult(problemDetails)
                    {
                        StatusCode = problemDetails.Status
                    };
                }
            );
        }

        private static ProblemDetails CreateProblemDetails(List<Error> errors)
        {
            var firstError = errors.FirstOrDefault();

            int statusCode = firstError.Type switch
            {
                ErrorType.Validation => StatusCodes.Status400BadRequest,
                ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
                ErrorType.Forbidden => StatusCodes.Status403Forbidden,
                ErrorType.NotFound => StatusCodes.Status404NotFound,
                ErrorType.Conflict => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status500InternalServerError
            };

            return new ProblemDetails
            {
                Title = firstError.Code ?? "An error occurred.",
                Status = statusCode,
                Detail = firstError.Description ?? "An unexpected error occurred.",
                Extensions =
                {
                    ["errorDetails"] = errors.GroupBy(e => e.Code)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToList())
                }
            };
        }
    }
}
