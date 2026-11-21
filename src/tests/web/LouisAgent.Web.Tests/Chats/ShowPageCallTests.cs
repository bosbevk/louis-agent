using LouisAgent.Web.Chats;

namespace LouisAgent.Web.Tests.Chats;

/// <summary>
/// Reading where a ShowPage call goes (F117, shared since F127.1): the guide's widget and a showroom's mock website
/// both move to the address the call names, and to nothing for any other call.
/// </summary>
public class ShowPageCallTests
{
    /// <summary>A tool call block, as a turn holds it.</summary>
    private static Block Call(string name, string? input) =>
        new()
        {
            Kind = BlockKind.Tool,
            ToolName = name,
            ToolInput = input
        };

    /// <summary>A ShowPage call goes to the address it names.</summary>
    [Test]
    public void AShowPageCall_GoesToItsAddress() =>
        Assert.That(ShowPageCall.PathOf(Call("ShowPage", "{\"path\":\"/rooms/garden\"}")), Is.EqualTo("/rooms/garden"));

    /// <summary>Any other tool, or a call without an address, goes nowhere.</summary>
    [TestCase("SendLead", "{\"path\":\"/rooms/garden\"}")]
    [TestCase("ShowPage", null)]
    [TestCase("ShowPage", "{}")]
    [TestCase("ShowPage", "{\"path\":7}")]
    [TestCase("ShowPage", "{\"path\":")]
    public void AnythingElse_GoesNowhere(string name, string? input) =>
        Assert.That(ShowPageCall.PathOf(Call(name, input)), Is.Null);
}
