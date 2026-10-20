using LouisAgent.AcpServer.Turns;

namespace LouisAgent.AcpServer.Tests.Turns;

// S (CL15): what a tool call looks like in Rider's chat: the card's kind, guessed from the tool's name so the client
// shows a fitting icon, and the output preview, cut so a long result never floods the card (the model still gets it all).
public class ToolCardsTests
{
    [TestCase("ReadNote", "read")]
    [TestCase("GetFileInfo", "read")]
    [TestCase("ListWorkspaceFiles", "read")]
    [TestCase("SearchWorkspace", "search")]
    [TestCase("WebSearch", "search")]
    [TestCase("DeleteFile", "delete")]
    [TestCase("RenameFileOrDirectory", "move")]
    [TestCase("WriteWorkspaceFile", "edit")]
    [TestCase("FetchUrl", "fetch")]
    [TestCase("DotNetBuild", "execute")]
    [TestCase("BashRun", "execute")]
    [TestCase("Ping", "other")]
    public void TheKind_ComesFromTheToolsNamePrefix(string tool, string kind) =>
        Assert.That(ToolCards.Kind(tool), Is.EqualTo(kind));

    [Test]
    public void AShortOutput_IsShownWhole() =>
        Assert.That(ToolCards.Preview("pong"), Is.EqualTo("pong"));

    [Test]
    public void ALongOutput_IsCut_WithHowMuchMoreThereIs()
    {
        string preview = ToolCards.Preview(new string('x', 2_000) + new string('y', 1_234));

        Assert.That(preview, Does.StartWith(new string('x', 2_000)));
        Assert.That(preview, Does.EndWith("\n... [1,234 more chars]"));
    }
}
