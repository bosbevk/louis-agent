using LouisAgent.Composition.Packs.Imports;
using LouisAgent.Composition.Packs.Screens;
using LouisAgent.Composition.Packs;
using LouisAgent.Core.Agent.Catalog;
using Microsoft.Extensions.FileProviders;

namespace LouisAgent.Testing;

/// <summary>
/// A pack for the catalogue's tests (CL15 step 1): one toolset built from <see cref="FakeShopTools"/> by default,
/// prompts held in memory, and whatever names and requirements a test gives it. No global state: each test builds its
/// own.
/// </summary>
public sealed class FakePack(
    string name = "fakeshop",
    IReadOnlyList<PackToolset>? toolsets = null,
    IReadOnlyDictionary<string, string>? prompts = null,
    IReadOnlyList<string>? requires = null,
    IReadOnlyList<IDataFeed>? feeds = null,
    IReadOnlyList<IDataImport>? imports = null,
    bool comesBeforeTheEngine = false,
    IReadOnlyList<IDataScreen>? screens = null) : IAgentPack
{
    public string Name => name;
    public bool ComesBeforeTheEngine => comesBeforeTheEngine;
    public IReadOnlyList<PackToolset> Toolsets { get; } = toolsets ?? [Toolset(name, typeof(FakeShopTools), _ => [new FakeShopTools()])];
    public IFileProvider Prompts { get; } = new FakePrompts(prompts ?? new Dictionary<string, string> { [$"toolsets/{name}.md"] = $"# {name}\n\nA guide.\n" });
    public IReadOnlyList<string> Requires => requires ?? [];
    public IReadOnlyList<IDataFeed> Feeds => feeds ?? [];
    public IReadOnlyList<IDataImport> Imports => imports ?? [];
    public IReadOnlyList<IDataScreen> Screens => screens ?? [];

    /// <summary>A toolset definition shaped like the catalogue's own, for a tool class a test names.</summary>
    public static PackToolset Toolset(string name, Type toolType, Func<IPackContext, IReadOnlyList<object>> create) =>
        new(name, $"The {name} toolset", GuideFile: $"toolsets/{name}.md", MissingSettings: _ => [], CreateTools: create, ToolTypes: [toolType]);
}
