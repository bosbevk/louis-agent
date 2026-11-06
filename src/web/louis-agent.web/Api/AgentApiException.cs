using System.Net;

namespace LouisAgent.Web.Api;

/// <summary>Thrown for an API error; its status lets the page react (401 = key, 404 = session gone).</summary>
public sealed class AgentApiException(HttpStatusCode status, string message) : Exception(message)
{
    public HttpStatusCode Status { get; } = status;
}
