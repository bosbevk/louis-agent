using System.Text.Json;

namespace OrderService;

/// <summary>
/// Records unhandled API exceptions as JSON lines in logs/errors.jsonl - a stand-in for an error tracker such as
/// Exceptionless, which the orchestrator watches.
/// </summary>
public static class ErrorLog
{
    // ERROR_LOG_PATH lets a caller (the orchestrator replaying a request) keep its own calls out of the production log.
    public static string FilePath { get; set; } = Environment.GetEnvironmentVariable("ERROR_LOG_PATH") ?? Path.Combine("logs", "errors.jsonl");

    public static string Record(string method, string arguments, Exception ex)
    {
        string id = Guid.NewGuid().ToString("N")[..12];
        var entry = new
        {
            id,
            timestamp = DateTimeOffset.UtcNow,
            service = "order-service",
            method,
            arguments,
            exception_type = ex.GetType().FullName,
            message = ex.Message,
            stack_trace = ex.StackTrace,
        };
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(FilePath))!);
        File.AppendAllText(FilePath, JsonSerializer.Serialize(entry) + Environment.NewLine);
        return id;
    }
}
