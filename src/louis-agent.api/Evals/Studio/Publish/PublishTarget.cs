using System.IO.Compression;

namespace LouisAgent.Api.Evals.Studio.Publish;

/// <summary>
/// Where a published eval goes (F49.5): beside the identity it proves, in its <c>Evals/</c> folder, under its name in
/// PascalCase as the eval folders are named (<c>Cook</c>, <c>Routing</c>), on the branch <c>eval/{identity}-{name}</c>;
/// and, for a host with no workspace, the same folder as a zip to download.
/// </summary>
/// <remarks>
/// A value with its own rules, so the publisher, the route and a test agree on one path (ADR 0058's folder, ADR 0027's
/// identity projects). The name and identity are kebab-case already (<c>ComposedEvals</c> held them to it), so the
/// paths they make can't climb out of the workspace.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0058-an-eval-is-a-folder-of-files.md">ADR 0058: an eval is a folder of files</seealso>
public sealed record PublishTarget(string Folder, string Branch, string FolderName)
{
    /// <summary>The target for an eval of this identity, by its name.</summary>
    public static PublishTarget Of(string identity, string name)
    {
        string folderName = string.Concat(name.Split('-').Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
        return new PublishTarget($"identities/louis-agent.identity.{identity}/Evals/{folderName}",
            $"eval/{identity}-{name}", folderName);
    }

    /// <summary>
    /// The identity's own project folder, which must be in the workspace for the eval to sit beside it.
    /// </summary>
    public string IdentityFolder => Folder[..Folder.IndexOf("/Evals/", StringComparison.Ordinal)];

    /// <summary>The folder's files as a zip, under the folder's name, paths with forward slashes.</summary>
    public byte[] Zip(string folder)
    {
        using var bytes = new MemoryStream();
        using (var zip = new ZipArchive(bytes, ZipArchiveMode.Create, leaveOpen: true))
            foreach (string file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories).Order())
            {
                string path = Path.GetRelativePath(folder, file).Replace('\\', '/');
                zip.CreateEntryFromFile(file, $"{FolderName}/{path}");
            }

        return bytes.ToArray();
    }
}
