using LouisAgent.Site.Resumes;
using LouisAgent.Site.Resumes.Sources;
using LouisAgent.Web.Components.Admin;

namespace LouisAgent.Site.Admin;

/// <summary>
/// The Resume area's card on the admin's home (F142): whose CV the site holds and how much is in it, with the way to
/// the editor.
/// </summary>
/// <remarks>
/// It reads the copy the page shows and draws what it last fetched (ADR 0042). Loading the key, a refusal and a lost
/// API are <see cref="AdminCardBase"/>'s to handle, as for every area's card.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0042-the-web-shows-the-live-results.md">ADR 0042: the web shows the live results</seealso>
public partial class ResumeCard
{
    private Resume? _resume;

    /// <summary>The CV the site holds now.</summary>
    protected override async Task ReadAsync() =>
        _resume = await Api.GetAsync<Resume>(ResumeAddress.Read, ResumeReader.Options);
}
