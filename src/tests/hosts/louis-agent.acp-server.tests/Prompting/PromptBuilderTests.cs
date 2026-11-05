using System.Text.Json;
using LouisAgent.AcpServer.Paths;
using LouisAgent.AcpServer.Prompting;

namespace LouisAgent.AcpServer.Tests.Prompting;

// S (CL15): the model's prompt from the ACP content blocks: text as it is, an embedded resource inlined, a file the user
// attached read from disk and capped, the file merely open in the IDE left out, and one that isn't mounted reported,
// never guessed.
public class PromptBuilderTests
{
    private string _tmp = null!;
    private PromptBuilder _builder = null!;

    [SetUp]
    public void SetUp()
    {
        _tmp = Directory.CreateTempSubdirectory("acp-prompt-").FullName.Replace('\\', '/');
        File.WriteAllText(Path.Combine(_tmp, "notes.md"), "Melktert on Sunday.");
        File.WriteAllText(Path.Combine(_tmp, "big.txt"), new string('x', 100_010));
        _builder = new PromptBuilder(new ContainerPaths(() => null, []));
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_tmp, recursive: true);

    private static JsonElement Params(string promptJson) => JsonDocument.Parse($$"""{"sessionId":"s","prompt":{{promptJson}}}""").RootElement;

    private string Link(string name, string? description = null) =>
        $$"""{"type":"resource_link","uri":"file:///{{_tmp}}/{{name}}","name":"{{name}}"{{(description is null ? "" : $$""","description":"{{description}}" """)}}}""";

    [Test]
    public void TextBlocks_AreJoined_AnEmbeddedResourceInlined()
    {
        string prompt = _builder.Build(Params("""[{"type":"text","text":"Look:"},{"type":"resource","resource":{"uri":"file:///x/a.cs","text":"class A;"}},{"type":"text","text":"Thanks."}]"""));

        Assert.That(prompt, Is.EqualTo("Look:\n\n[Attached file: a.cs]\n```\nclass A;\n```\n\nThanks."));
    }

    [Test]
    public void AnAttachedFile_IsRead_TheOneOpenInTheIde_IsNot()
    {
        string prompt = _builder.Build(Params($"[{Link("notes.md")},{Link("Open.cs", "File opened in the IDE")}]"));

        Assert.That(prompt, Is.EqualTo($"[Attached file: notes.md ({_tmp}/notes.md)]\n```\nMelktert on Sunday.\n```"));
    }

    [Test]
    public void ALargeAttachment_IsCapped_AndSaysSo()
    {
        string prompt = _builder.Build(Params($"[{Link("big.txt")}]"));

        Assert.That(prompt, Does.Contain(new string('x', 100_000) + "\n```"));
        Assert.That(prompt, Does.EndWith("[Attachment truncated: showing 100,000 of 100,010 chars]"));
    }

    [Test]
    public void AFileThatIsntMounted_IsReported_NeverGuessed()
    {
        string prompt = _builder.Build(Params("""[{"type":"resource_link","uri":"file:///C:/nowhere/secret.txt","name":"secret.txt"}]"""));

        Assert.That(prompt, Is.EqualTo("[Attached file: secret.txt could not be read - it is not inside a folder mounted into the agent (file:///C:/nowhere/secret.txt)]"));
    }

    [Test]
    public void NoPromptBlocks_IsEmpty() =>
        Assert.That(_builder.Build(Params("[]")), Is.Empty);

    [Test]
    public void TheFirstFileLink_IsTheActiveFile_Unescaped()
    {
        var parameters = Params("""[{"type":"text","text":"hi"},{"type":"resource_link","uri":"file:///C:/My%20Code/App.cs","name":"App.cs"}]""");

        Assert.That(PromptBuilder.ResourceLinkPath(parameters), Is.EqualTo("C:/My Code/App.cs"));
        Assert.That(PromptBuilder.ResourceLinkPath(Params("[]")), Is.Null);
    }
}
