using LouisAgent.Core.Agent;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace LouisAgent.Core.Tests.Agent;

/// <summary>
/// A pack for the catalogue's tests (CL15 step 1): one toolset built from <see cref="FakeShopTools"/> by default, prompts
/// held in memory, and whatever names and requirements a test gives it. No global state: each test builds its own.
/// </summary>
internal sealed class FakePack(
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
        new(name, $"The {name} toolset", GuideFile: $"toolsets/{name}.md", MissingSettings: _ => [], CreateTools: create, ToolType: toolType);
}

/// <summary>An identity for the catalogue's tests: a name, in-memory prompts and the packs it names.</summary>
internal sealed class FakeIdentity(string name = "fakeid", IReadOnlyList<string>? packs = null, IReadOnlyDictionary<string, string>? prompts = null) : IAgentIdentity
{
    public string Name => name;
    public IFileProvider Prompts { get; } = new FakePrompts(prompts ?? new Dictionary<string, string> { [$"agent/{name}/voice.md"] = $"# {name}\n\nA voice.\n" });
    public IReadOnlyList<string> Packs => packs ?? [];
}

/// <summary>The pack's tools: public methods become tools, as in a real pack.</summary>
public sealed class FakeShopTools
{
    [System.ComponentModel.Description("Finds fake products.")]
    public string FindFakeProducts([System.ComponentModel.Description("What to find")] string query) => $"found {query}";
}

/// <summary>A tool class whose tool takes a name that is already core's (<c>FetchUrl</c> is the web toolset's).</summary>
public sealed class FakeClashingTools
{
    [System.ComponentModel.Description("Takes the web toolset's name.")]
    public string FetchUrl(string url) => url;
}

/// <summary>A tool class whose tool takes an engine tool's name (<c>GetToday</c> is the clock's, registered by the engine).</summary>
public sealed class FakeEngineNameTools
{
    [System.ComponentModel.Description("Takes the engine's name.")]
    public string GetToday() => "";
}

/// <summary>
/// Prompts in memory, as a pack or identity would embed them: paths with forward slashes under their <c>Prompts/</c>,
/// directories implied by the paths. Enough of <see cref="IFileProvider"/> for the catalogue and the prompt reader.
/// </summary>
internal sealed class FakePrompts(IReadOnlyDictionary<string, string> files) : IFileProvider
{
    public IFileInfo GetFileInfo(string subpath)
    {
        string path = subpath.TrimStart('/');
        return files.TryGetValue(path, out string? text) ? new File(Path.GetFileName(path), text) : new NotFound(path);
    }

    public IDirectoryContents GetDirectoryContents(string subpath)
    {
        string folder = subpath.Trim('/');
        string prefix = folder.Length == 0 ? "" : folder + "/";
        var entries = new List<IFileInfo>();
        foreach (string path in files.Keys.Where(p => p.StartsWith(prefix, StringComparison.Ordinal)).Order(StringComparer.Ordinal))
        {
            string rest = path[prefix.Length..];
            int slash = rest.IndexOf('/');
            if (slash < 0) entries.Add(new File(rest, files[path]));
            else if (entries.All(e => e.Name != rest[..slash])) entries.Add(new Directory(rest[..slash]));
        }
        return entries.Count == 0 && folder.Length > 0 ? NotFoundDirectoryContents.Singleton : new Contents(entries);
    }

    public IChangeToken Watch(string filter) => NullChangeToken.Singleton;

    private sealed class Contents(List<IFileInfo> entries) : IDirectoryContents
    {
        public bool Exists => true;
        public IEnumerator<IFileInfo> GetEnumerator() => entries.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => entries.GetEnumerator();
    }

    private sealed class File(string name, string text) : IFileInfo
    {
        public bool Exists => true;
        public long Length => System.Text.Encoding.UTF8.GetByteCount(text);
        public string? PhysicalPath => null;
        public string Name => name;
        public DateTimeOffset LastModified => DateTimeOffset.MinValue;
        public bool IsDirectory => false;
        public Stream CreateReadStream() => new MemoryStream(System.Text.Encoding.UTF8.GetBytes(text));
    }

    private sealed class Directory(string name) : IFileInfo
    {
        public bool Exists => true;
        public long Length => -1;
        public string? PhysicalPath => null;
        public string Name => name;
        public DateTimeOffset LastModified => DateTimeOffset.MinValue;
        public bool IsDirectory => true;
        public Stream CreateReadStream() => throw new InvalidOperationException();
    }

    private sealed class NotFound(string name) : IFileInfo
    {
        public bool Exists => false;
        public long Length => -1;
        public string? PhysicalPath => null;
        public string Name => name;
        public DateTimeOffset LastModified => DateTimeOffset.MinValue;
        public bool IsDirectory => false;
        public Stream CreateReadStream() => throw new FileNotFoundException(name);
    }
}
