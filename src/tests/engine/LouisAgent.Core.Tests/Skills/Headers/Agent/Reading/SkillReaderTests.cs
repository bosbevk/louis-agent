using LouisAgent.Core.Skills.Languages;
using LouisAgent.Core.Skills.Headers.Agent;
using LouisAgent.Core.Skills.Headers.Agent.Reading;

namespace LouisAgent.Core.Tests.Skills.Headers.Agent.Reading;

/// <summary>
/// F97.1 step 1: a skill the agent made, or one dropped in by hand, is a folder: its <c>SKILL.md</c> in the Agent
/// Skills shape naming its script, its <c>parameters.schema.json</c>, and the script. The reader makes it a skill, or
/// refuses it saying what to change, so a broken skill never reaches the model.
/// </summary>
public class SkillReaderTests
{
    private const string Header =
        "---\nname: word-count\ndescription: Counts words.\nmetadata:\n  script: scripts/count.py\n---\n";
    private const string Body = "# Count words\n\nCounts the words in a text.\n";

    private const string Schema = """
                                  {
                                    "type": "object",
                                    "properties": {
                                      "text": { "type": "string", "description": "The text." },
                                      "limit": { "type": "integer", "description": "At most this many." }
                                    },
                                    "required": ["text"]
                                  }
                                  """;

    private string _skills = "";

    /// <summary>A folder of skills of its own for each test.</summary>
    [SetUp]
    public void MakeTheSkillsFolder() => _skills = Directory.CreateTempSubdirectory("skill-reader-").FullName;

    /// <summary>The folder goes with the test.</summary>
    [TearDown]
    public void RemoveTheSkillsFolder() => Directory.Delete(_skills, recursive: true);

    /// <summary>
    /// A good folder is a skill: its header's fields, the body as its instructions, its script and its parameters.
    /// </summary>
    [Test]
    public void AGoodFolder_IsASkill()
    {
        string folder = Write("word-count", Header + Body, Schema, ("scripts/count.py", "print(1)"));

        var skill = SkillReader.Read(folder, SkillState.Drafted);

        Assert.Multiple(() =>
        {
            Assert.That((skill.Name, skill.Description, skill.State),
                Is.EqualTo(("word-count", "Counts words.", SkillState.Drafted)));
            Assert.That(skill.Instructions, Does.Contain("Counts the words in a text."));
            Assert.That((skill.Script, skill.Language),
                Is.EqualTo((Path.Combine(folder, "scripts", "count.py"), ScriptLanguage.Python)));
            Assert.That(skill.Parameters.Types, Is.EquivalentTo(new Dictionary<string, SkillParameterType>
            {
                ["text"] = SkillParameterType.String,
                ["limit"] = SkillParameterType.Integer
            }));
            Assert.That(skill.Parameters.Required, Is.EqualTo(new[] { "text" }));
        });
    }

    /// <summary>Each language the runner has is known by its script's extension.</summary>
    [TestCase("scripts/run.py", ScriptLanguage.Python)]
    [TestCase("scripts/run.ps1", ScriptLanguage.PowerShell)]
    [TestCase("scripts/run.sh", ScriptLanguage.Bash)]
    public void AScriptsExtension_SaysItsLanguage(string script, ScriptLanguage expected)
    {
        string header = Header.Replace("scripts/count.py", script);
        string folder = Write("word-count", header + Body, Schema, (script, "echo 1"));

        Assert.That(SkillReader.Read(folder, SkillState.Approved).Language, Is.EqualTo(expected));
    }

    /// <summary>What is wrong with a folder refuses it, naming the skill and what to change.</summary>
    [TestCase("no-skill-file", "SKILL.md")]
    [TestCase("header-unread", "toolset")]
    [TestCase("script-not-named", "metadata.script")]
    [TestCase("script-outside", "outside")]
    [TestCase("script-missing", "scripts/count.py")]
    [TestCase("script-language", ".rb")]
    [TestCase("schema-missing", "parameters.schema.json")]
    [TestCase("schema-not-object", "object")]
    [TestCase("parameter-untyped", "'text'")]
    public void AFaultyFolder_IsRefused(string fault, string named)
    {
        string folder = Faulty(fault);

        var refused = Assert.Throws<SkillRefusedException>(() => SkillReader.Read(folder, SkillState.Drafted));

        Assert.That(refused!.Message, Does.Contain("word-count").And.Contain(named));
    }

    /// <summary>A folder with one thing wrong, the rest as a good one has it.</summary>
    private string Faulty(string fault)
    {
        var script = ("scripts/count.py", "print(1)");
        return fault switch
        {
            "no-skill-file" => Write("word-count", null, Schema, script),
            "header-unread" => Write("word-count", Header.Replace("metadata:", "toolset: files\nmetadata:") + Body,
                Schema, script),
            "script-not-named" => Write("word-count",
                "---\nname: word-count\ndescription: Counts words.\n---\n" + Body, Schema, script),
            "script-outside" => Write("word-count", Header.Replace("scripts/count.py", "../count.py") + Body,
                Schema, script),
            "script-missing" => Write("word-count", Header + Body, Schema),
            "script-language" => Write("word-count", Header.Replace("count.py", "count.rb") + Body, Schema,
                ("scripts/count.rb", "puts 1")),
            "schema-missing" => Write("word-count", Header + Body, null, script),
            "schema-not-object" => Write("word-count", Header + Body, "[1, 2]", script),
            "parameter-untyped" => Write("word-count", Header + Body,
                """{ "type": "object", "properties": { "text": { "description": "The text." } } }""", script),
            _ => throw new ArgumentOutOfRangeException(nameof(fault), fault, null)
        };
    }

    /// <summary>A skill's folder with these files; a missing one is left out.</summary>
    private string Write(string name, string? skillFile, string? schema, params (string Path, string Text)[] files)
    {
        string folder = Path.Combine(_skills, name);
        Directory.CreateDirectory(folder);
        if (skillFile is not null) File.WriteAllText(Path.Combine(folder, "SKILL.md"), skillFile);
        if (schema is not null) File.WriteAllText(Path.Combine(folder, "parameters.schema.json"), schema);

        foreach (var (path, text) in files)
        {
            string file = Path.Combine(folder, path);
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.WriteAllText(file, text);
        }

        return folder;
    }
}
