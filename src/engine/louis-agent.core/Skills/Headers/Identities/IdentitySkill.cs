using LouisAgent.Composition.Messages;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Skills.Messages;

namespace LouisAgent.Core.Skills.Headers.Identities;

/// <summary>
/// One of an identity's skills (F129): its name as the model asks for it (<c>louis-agent-guide/site-map</c>), the line
/// that says when it's needed, whether it's always in the prompt or read on demand, and the text the model reads.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0114-an-identitys-skills-load-always-or-on-demand.md">ADR 0114: an identity's skills load always or on demand</seealso>
public sealed record IdentitySkill(string Name, string Description, SkillLoad Load, string Body)
{
    /// <summary>
    /// The skill in a file of an identity's skills folder. A file there that doesn't say how it loads refuses, since
    /// it would otherwise be neither in the prompt nor listed.
    /// </summary>
    public static IdentitySkill Of(string identity, PromptFile file) =>
        file.Header is { Load: { } load } header
            ? new IdentitySkill($"{identity}/{header.Name}", header.Description, load, file.Body)
            : throw new PromptHeaderException(
                MessageText.Format(PromptHeaderMessage.LoadMissing, new { Path = file.Path }));
}
