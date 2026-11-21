using LouisAgent.Api.Contracts.Info;
using LouisAgent.Core.Hosting;

namespace LouisAgent.Api.Endpoints.Info;

/// <summary>Builds the list of identities this instance runs (F109.3): what a session may open as.</summary>
/// <remarks>
/// Replace Constructor with Factory Function (Fowler), as a C# 14 static extension member, as
/// <c>InfoReplyFactories</c>. A class of its own, since a static extension member compiles to a plain static method on
/// its class and <c>RolesReply.Of(AgentEngineFactory)</c> already has that signature there.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
internal static class IdentitiesReplyFactory
{
    /// <summary>The identities this instance runs, the first as the default.</summary>
    extension(IdentitiesReply)
    {
        /// <summary>One row per identity of the catalogue, in its order.</summary>
        public static IdentitiesReply Of(AgentEngineFactory engines) => new(
        [
            .. engines.Catalog.Identities.Select((identity, at) =>
                new IdentityRow(identity.Name, identity.DefaultVoice, identity.Packs, at == 0))
        ]);
    }
}
