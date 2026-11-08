using LouisAgent.Choices;

namespace LouisAgent.Identities.Triage;

/// <summary>
/// The packs the triage team names (ADR 0043: an identity keeps its own enum and never references a pack): the triage
/// pack for the typed endings, and the files, git and dotnet packs the investigator and the reviewer read the code
/// and run the tests with. Nothing that writes code: that is the coder's, on its own peer.
/// </summary>
public enum TriageIdentityPack
{
    [WireName("triage")] Triage,
    [WireName("files")] Files,
    [WireName("git")] Git,
    [WireName("dotnet")] DotNet,
}
