using LouisAgent.Composition.Messages;
using LouisAgent.Packs.Triage.Messages;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace LouisAgent.Packs.Triage.Departments;

/// <summary>
/// Reads the departments from their YAML file: a list under <c>departments</c>, each with its name, repository,
/// error log and people folder, every path made absolute against the file's own folder. A name that isn't a plain
/// token, or one that repeats, is refused: a name ends up in folders and run ids.
/// </summary>
/// <remarks>
/// YamlDotNet with the file's snake_case keys (the same reader the eval files use); the file's shape is one private
/// record per level (ADR 0041), never a dictionary picked apart by name.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public static class DepartmentsFile
{
    private static readonly IDeserializer Yaml = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    /// <summary>The departments in the file, in its order, paths absolute.</summary>
    public static IReadOnlyList<Department> Read(string yamlPath)
    {
        string folder = Path.GetDirectoryName(Path.GetFullPath(yamlPath))!;
        var file = Yaml.Deserialize<Shape>(File.ReadAllText(yamlPath)) ?? new Shape();
        var departments = file.Departments.Select(entry => entry.ToDepartment(folder)).ToList();

        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var department in departments)
        {
            if (!PlainId.Is(department.Name) || !names.Add(department.Name))
                throw new InvalidDataException(MessageText.Format(TriageToolMessage.BadDepartment,
                    new
                    {
                        Name = department.Name,
                        File = yamlPath
                    }));
        }

        return departments;
    }

    /// <summary>The file's top: its one list.</summary>
    private sealed class Shape
    {
        public List<Entry> Departments { get; set; } = [];
    }

    /// <summary>One entry as written, paths still relative to the file.</summary>
    private sealed class Entry
    {
        public string Name { get; set; } = "";
        public string Repository { get; set; } = "";
        public string ErrorLog { get; set; } = "";
        public string People { get; set; } = "";

        /// <summary>The department, every path made absolute against the file's folder.</summary>
        public Department ToDepartment(string folder) =>
            new(Name.Trim(), Absolute(folder, Repository), Absolute(folder, ErrorLog), Absolute(folder, People));

        private static string Absolute(string folder, string path) =>
            Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(folder, path));
    }
}
