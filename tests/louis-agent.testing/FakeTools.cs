using System.ComponentModel;

namespace LouisAgent.Testing;

/// <summary>The fake pack's tools: public methods become tools, as in a real pack.</summary>
public sealed class FakeShopTools
{
    [Description("Finds fake products.")]
    public string FindFakeProducts([Description("What to find")] string query) => $"found {query}";
}

/// <summary>A tool class whose tool takes a name that is already core's (<c>FetchUrl</c> is the web toolset's).</summary>
public sealed class FakeClashingTools
{
    [Description("Takes the web toolset's name.")]
    public string FetchUrl(string url) => url;
}

/// <summary>A tool class whose tool takes an engine tool's name (<c>GetToday</c> is the clock's, registered by the engine).</summary>
public sealed class FakeEngineNameTools
{
    [Description("Takes the engine's name.")]
    public string GetToday() => "";
}
