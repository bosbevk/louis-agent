using LouisAgent.Composition.Guardrails.Workspace;
using LouisAgent.Packs.Triage.Departments;

namespace LouisAgent.Packs.Triage.Tests.Departments;

/// <summary>
/// Departments are configuration (F106.3.1): read from their YAML with every path made absolute against the file, a
/// bad or repeated name refused; and a department's coder is held to its repository by the workspace boundary, so
/// one department can't reach another's.
/// </summary>
public class DepartmentsTests
{
    private string _dir = null!;

    [SetUp]
    public void MakeAFolder()
    {
        _dir = Path.Combine(Path.GetTempPath(), $"triage-departments-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(_dir, "repos", "code"));
        Directory.CreateDirectory(Path.Combine(_dir, "repos", "ops"));
    }

    [TearDown]
    public void RemoveTheFolder() => Directory.Delete(_dir, recursive: true);

    private string Write(string yaml)
    {
        string path = Path.Combine(_dir, "departments.yaml");
        File.WriteAllText(path, yaml);
        return path;
    }

    private const string TwoDepartments = """
                                          departments:
                                            - name: code
                                              repository: repos/code
                                              error_log: repos/code/logs/errors.jsonl
                                              people: people/code
                                            - name: ops
                                              repository: repos/ops
                                              error_log: repos/ops/logs/errors.jsonl
                                              people: people/ops
                                          """;

    /// <summary>The file's departments, in order, every path absolute against the file's folder.</summary>
    [Test]
    public void TheFile_GivesItsDepartments_WithAbsolutePaths()
    {
        var departments = DepartmentsFile.Read(Write(TwoDepartments));

        Assert.That(departments.Select(d => d.Name), Is.EqualTo(new[] { "code", "ops" }));
        Assert.That(departments[0].Repository, Is.EqualTo(Path.GetFullPath(Path.Combine(_dir, "repos", "code"))));
        Assert.That(departments[1].People, Is.EqualTo(Path.GetFullPath(Path.Combine(_dir, "people", "ops"))));
    }

    /// <summary>A name that isn't a plain token, or one that repeats, is refused with the file named.</summary>
    [TestCase("departments:\n  - name: 'code; rm'\n    repository: repos/code\n    error_log: x\n    people: y\n")]
    [TestCase(
        "departments:\n  - name: code\n    repository: a\n    error_log: b\n    people: c\n  - name: code\n    repository: d\n    error_log: e\n    people: f\n")]
    public void ABadOrRepeatedName_IsRefused(string yaml)
    {
        string path = Write(yaml);

        var refusal = Assert.Throws<InvalidDataException>(() => DepartmentsFile.Read(path));

        Assert.That(refusal!.Message, Does.Contain("departments.yaml"));
    }

    /// <summary>A department's coder cannot touch another department's repository: the boundary refuses the path.</summary>
    [Test]
    public void ADepartmentsCoder_CannotTouchAnotherDepartmentsRepository()
    {
        var departments = DepartmentsFile.Read(Write(TwoDepartments));
        var code = new WorkspaceBoundary(departments[0].Repository);
        File.WriteAllText(Path.Combine(departments[1].Repository, "appsettings.json"), "{}");

        Assert.Multiple(() =>
        {
            Assert.That(code.Resolve("Api/OrderTotal.cs"), Does.StartWith(departments[0].Repository));
            Assert.Throws<ArgumentException>(() => code.Resolve("../ops/appsettings.json"));
            Assert.Throws<ArgumentException>(() =>
                code.Resolve(Path.Combine(departments[1].Repository, "appsettings.json")));
        });
    }

    /// <summary>The demo's own file reads: two departments, the code one on the sample service.</summary>
    [Test]
    public void TheDemosFile_Reads()
    {
        var departments =
            DepartmentsFile.Read(Path.Combine(TestPaths.RepoRoot, "demos/louis-agent/config/departments.yaml"));

        Assert.That(departments.Select(d => d.Name), Is.EqualTo(new[] { "code", "ops" }));
        Assert.That(departments[0].Repository.Replace('\\', '/'),
            Does.EndWith("demos/louis-agent/samples/order-service"));
    }
}
