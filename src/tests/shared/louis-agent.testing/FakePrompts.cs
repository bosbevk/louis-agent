using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace LouisAgent.Testing;

/// <summary>
/// Prompts in memory, as a pack or identity would embed them: paths with forward slashes under their <c>Prompts/</c>,
/// directories implied by the paths. Enough of <see cref="IFileProvider"/> for the catalogue and the prompt reader.
/// </summary>
public sealed class FakePrompts(IReadOnlyDictionary<string, string> files) : IFileProvider
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
