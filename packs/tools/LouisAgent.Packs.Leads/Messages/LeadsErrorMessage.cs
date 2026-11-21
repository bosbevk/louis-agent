using System.ComponentModel;

namespace LouisAgent.Packs.Leads.Messages;

/// <summary>What the sender says when it couldn't send (ADR 0009); the tool hands it to the model as the reason.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
public enum LeadsErrorMessage
{
    [Description("{Status} from the mail service: {Body}")]
    ServiceSaidNo,
}
