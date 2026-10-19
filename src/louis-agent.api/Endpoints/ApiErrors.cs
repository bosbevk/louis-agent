namespace LouisAgent.Api.Endpoints;

internal static class ApiErrors
{
    public static object Body(string type, string message) => new { type = "error", error = new { type, message } };

    public static IResult Result(int status, string type, string message) => Results.Json(Body(type, message), statusCode: status);
}
