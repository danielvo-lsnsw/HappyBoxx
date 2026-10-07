using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace HappyBoxx.BuildingBlocks.Results;

public static class ResultExtensions
{
    public static ProblemHttpResult ToProblem(this Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError,
        };

        return TypedResults.Problem(
            statusCode: statusCode,
            title: error.Type.ToString(),
            detail: error.Description,
            extensions: new Dictionary<string, object?> { ["errorCode"] = error.Code });
    }
}
