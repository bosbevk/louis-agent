using LouisAgent.Core.Agent.Catalog;

namespace LouisAgent.Testing;

/// <summary>A feed for the catalogue's and the API's tests: a name and the JSON a test gives it, read as it is.</summary>
public sealed class FakeFeed(string name, string json = "{}") : IDataFeed
{
    /// <inheritdoc />
    public string Name => name;

    /// <inheritdoc />
    public string Description => $"The {name} feed";

    /// <inheritdoc />
    public Task<string> ReadJsonAsync(CancellationToken cancellationToken) => Task.FromResult(json);
}
