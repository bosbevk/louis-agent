using LouisAgent.Composition.Agents;
using LouisAgent.Composition.Messages;
using LouisAgent.Composition.Packs;
using LouisAgent.Core.Delegation.Messages;

namespace LouisAgent.Core.Delegation.Addresses;

/// <summary>
/// Who may call whom: an identity asks only the agents its <see cref="IAgentIdentity.Calls"/> names, whatever the
/// model writes; any other address is refused with the ones it may call.
/// </summary>
/// <remarks>
/// An allow-list held in code (ADR 0004: the model's words are data, never permission): the identity's own list says
/// whom it may reach, so no prompt can widen it. An address is matched whole, so naming <c>acme.cook</c> lets the
/// identity ask the cook, not acme in its own voice.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0117-one-address-for-every-agent-call.md">ADR 0117: one address for every agent call</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0004-arguments-as-data.md">ADR 0004: arguments as data</seealso>
public static class AgentCalls
{
    /// <summary>Whether the asker may call this address, and the reason the model reads when not.</summary>
    public static bool Allows(IAgentIdentity asker, AgentAddress address, out string? why)
    {
        // The list as addresses, read the same way the model's address was, so the case never matters.
        var calls = asker.Calls.Select(call => AgentAddress.TryParse(call, out var listed) ? listed : null)
            .OfType<AgentAddress>().ToList();
        why = null;
        if (calls.Contains(address)) return true;

        why = MessageText.Format(SubSessionToolMessage.AddressNotCallable, new
        {
            Asker = asker.Name,
            Address = address.ToString(),
            Calls = calls.Count == 0 ? "(none)" : string.Join(", ", calls.Select(call => call.ToString()))
        });
        return false;
    }
}
