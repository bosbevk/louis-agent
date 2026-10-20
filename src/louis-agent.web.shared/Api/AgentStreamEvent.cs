using System.Net;
using System.Net.Http.Json;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace LouisAgent.Web.Shared.Api;

/// <summary>One streamed event: its type (content_block_delta, message_stop, ...) and its JSON data.</summary>
public sealed record AgentStreamEvent(string Type, JsonElement Data);
