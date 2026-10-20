using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Agent.Catalog;
using Microsoft.Extensions.FileProviders;

namespace LouisAgent.Testing;

/// <summary>
/// A pack for the catalogue's tests (CL15 step 1): one toolset built from <see cref="FakeShopTools"/> by default, prompts
/// held in memory, and whatever names and requirements a test gives it. No global state: each test builds its own.
/// </summary>
public sealed class FakePack(
    string name = "fakeshop",
    IReadOnlyList<ToolsetDefinition>? toolsets = null,
    IReadOnlyDictionary<string, string>? prompts = null,
    IReadOnlyList<string>? requires = null) : IAgentPack
{
    public string Name => name;
    public IReadOnlyList<ToolsetDefinition> Toolsets { get; } = toolsets ?? [Toolset(name, typeof(FakeShopTools), _ => [new FakeShopTools()])];
    public IFileProvider Prompts { get; } = new FakePrompts(prompts ?? new Dictionary<string, string> { [$"toolsets/{name}.md"] = $"# {name}\n\nA guide.\n" });
    public IReadOnlyList<string> Requires => requires ?? [];

    /// <summary>A toolset definition shaped like the catalogue's own, for a tool class a test names.</summary>
    public static ToolsetDefinition Toolset(string name, Type toolType, Func<ToolsetContext, IReadOnlyList<object>> create) =>
        new(name, $"The {name} toolset", GuideFile: $"toolsets/{name}.md", MissingSettings: _ => [], CreateTools: create, ToolTypes: [toolType]);
}
