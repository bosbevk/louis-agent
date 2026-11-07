using LouisAgent.McpServer.Transport;

namespace LouisAgent.McpServer.Tests.Transport;

// MCP_TRANSPORT as an enum (ADR 0024: a choice is parsed once where it enters). Unset is stdio, the way a local client
// starts the server; a value that names nothing stops start-up instead of falling back to stdio without a word.
public class McpTransportSettingTests
{
    [TestCase(null, McpTransport.Stdio)]
    [TestCase("", McpTransport.Stdio)]
    [TestCase("   ", McpTransport.Stdio)]
    [TestCase("stdio", McpTransport.Stdio)]
    [TestCase("http", McpTransport.Http)]
    [TestCase(" HTTP ", McpTransport.Http)]
    public void TheTransport_IsStdioUnlessHttpIsSaid(string? setting, McpTransport expected) =>
        Assert.That(McpTransportSetting.Parse(setting), Is.EqualTo(expected));

    [TestCase("https")]
    [TestCase("1")]
    public void AnUnknownTransport_NamesTheSettingAndItsValues(string setting)
    {
        var error = Assert.Throws<ArgumentException>(() => McpTransportSetting.Parse(setting))!;
        Assert.That(error.Message,
            Does.Contain("MCP_TRANSPORT").And.Contain(setting).And.Contain("stdio").And.Contain("http"));
    }
}
