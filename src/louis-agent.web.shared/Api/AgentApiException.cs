using System.Net;
using System.Net.Http.Json;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace LouisAgent.Web.Shared.Api;

/// <summary>Thrown for an API error response; <see cref="Status"/> lets the UI react (401 = key, 404 = session gone).</summary>
public sealed class AgentApiException(HttpStatusCode status, string message) : Exception(message)
{
    public HttpStatusCode Status { get; } = status;
}
