namespace LouisAgent.Acp.Protocol;

/// <summary>
/// The error codes JSON-RPC 2.0 sets aside, by the names its specification gives them, so an error we send says what
/// went wrong in a way every client reads the same.
/// </summary>
/// <seealso href="https://www.jsonrpc.org/specification#error_object">JSON-RPC 2.0: the error object</seealso>
internal static class JsonRpcErrorCodes
{
    /// <summary>The line we got wasn't JSON.</summary>
    public const int ParseError = -32700;

    /// <summary>We don't have the method asked for.</summary>
    public const int MethodNotFound = -32601;

    /// <summary>The method's parameters are wrong: an unknown session, a version we don't speak.</summary>
    public const int InvalidParams = -32602;

    /// <summary>Something failed on our side while we handled the request.</summary>
    public const int InternalError = -32603;
}
