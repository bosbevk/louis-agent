using System.Text.Json;
using LouisAgent.Api.Evals.Drafts.Composed;
using LouisAgent.Api.Evals.Drafts.Samples;
using LouisAgent.Api.Evals.Drafts.Store;
using LouisAgent.Api.Evals.Drafts.Transcripts;
using LouisAgent.Evals.Protocol.Parts.Conversations;

namespace LouisAgent.Api.Testing;

/// <summary>
/// What every store of drafts does, in memory and in MongoDB alike. A draft comes back with everything it was made
/// with, a transcript's lines whole; an eval's drafts come back oldest first, samples and transcripts each in their
/// list, and never another eval's; a draft is found and deleted by its id alone, and the delete says whether it was
/// there. Every test makes its own eval names and ids, so tests can share a real database without seeing each other's
/// drafts; times are whole milliseconds in UTC, as MongoDB keeps them.
/// </summary>
/// <remarks>
/// Abstract test (a contract test): the tests are written once, and each store gets a small subclass that only says
/// how to make one, so the in-memory store and the Mongo store are held to the same rules.
/// </remarks>
public abstract class EvalDraftStoreContract
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 1, 9, 0, 0, 125, TimeSpan.Zero);

    /// <summary>The conversation a draft comes from.</summary>
    private static readonly ConversationKey CookAf = new(SampleMode.Beta, 1, "cook-af");

    /// <summary>The identity the drafts' eval belongs to; any name does, the store only keeps it.</summary>
    private const string Identity = "identity-a";

    private IEvalDraftStore _store = null!;

    /// <summary>A store to test; each test gets a fresh one (a real store may share its database).</summary>
    protected abstract IEvalDraftStore CreateStore();

    /// <summary>A fresh store before every test.</summary>
    [SetUp]
    public void SetUpStore() => _store = CreateStore();

    /// <summary>An eval name no other test uses, so tests sharing a database never meet.</summary>
    private static string NewEval() => "eval-" + Guid.NewGuid().ToString("N")[..12];

    /// <summary>A draft id no other draft has.</summary>
    private static string NewId() => Guid.NewGuid().ToString("N")[..12];

    /// <summary>
    /// A sample draft for the eval, some seconds after the start: a bobotie for four in Malmesbury, asked in
    /// Afrikaans, with a follow-up and no ideal answer, opening in its own role. Its expected values hold text, a number,
    /// true and null.
    /// </summary>
    private static SampleDraft Sample(string eval, int seconds, string? target = null, string? place = "Malmesbury") =>
        new(NewId(), eval, Identity, "run123456789", CookAf,
            new SampleBody("cook-bobotie-four",
                [new SampleBodyMessage("user", "Gee my 'n resep vir bobotie vir vier.")],
                new SampleBodyMetadata(place, "Africa/Johannesburg", ["En 'n slaai daarby?"],
                    JsonDocument.Parse(
                            """{"routes_to":"puls-cook","serves":4,"budget":250.5,"recipe_image":true,"shop":null}""")
                        .RootElement.Clone(),
                    ["language:af", "kind:cook"], "puls/voice-af"), target),
            "Louis", T0.AddSeconds(seconds));

    /// <summary>A known-bad transcript draft for the eval, some seconds after the start, holding these lines.</summary>
    private static TranscriptDraft KnownBad(string eval, int seconds, params string[] lines) =>
        new(NewId(), eval, Identity, "run123456789", CookAf, TranscriptKind.Bad, "side-dish",
            "cook-af.bad.side-dish.txt", lines, "Louis", T0.AddSeconds(seconds));

    /// <summary>
    /// Turns drafts into JSON to compare them, since a record's own equality compares its lists by reference.
    /// </summary>
    private static string[] Json<T>(IEnumerable<T> drafts) => [.. drafts.Select(d => JsonSerializer.Serialize(d))];

    /// <summary>A sample draft comes back with everything it was made with, its expected values as they were.</summary>
    [Test]
    public async Task ASampleDraft_ReadsBackWithEverythingItWasMadeWith()
    {
        string eval = NewEval();
        var draft = Sample(eval, 1);

        await _store.AddAsync(draft);

        var drafts = await _store.OfEvalAsync(eval);
        Assert.That(Json(drafts.Samples), Is.EqualTo(Json([draft])));
        Assert.That(drafts.Transcripts, Is.Empty);
    }

    /// <summary>A sample draft with an ideal answer and no place keeps both as they were.</summary>
    [Test]
    public async Task ASampleDraftWithATargetAndNoPlace_ReadsBackTheSame()
    {
        string eval = NewEval();
        var draft = Sample(eval, 1, "Bobotie vir vier.", null);

        await _store.AddAsync(draft);

        Assert.That(Json((await _store.OfEvalAsync(eval)).Samples), Is.EqualTo(Json([draft])));
    }

    /// <summary>
    /// A transcript's lines come back whole, each one as it was: JSON with its quotes and escapes, Afrikaans and the
    /// ± of an estimate, and a line as long as a full answer.
    /// </summary>
    [Test]
    public async Task ATranscriptDraft_KeepsItsLinesWhole()
    {
        string eval = NewEval();
        string longAnswer = string.Concat(Enumerable.Repeat("| Uie | 2 | uie 1 kg | ± R25 |\\n", 2000));
        var draft = KnownBad(eval, 1,
            """{"type":"turn_start","task":"cook-af","turn":1}""",
            """{"type":"text","text":"Hier is jou resep vir bobotie — \"lekker\" vir vier.\n\tSkottel: ê, ë, ±"}""",
            "{\"type\":\"text\",\"text\":\"" + longAnswer + "\"}",
            """{"type":"turn_end","task":"cook-af","turn":1}""");

        await _store.AddAsync(draft);

        var drafts = await _store.OfEvalAsync(eval);
        Assert.That(drafts.Transcripts.Single().Lines, Is.EqualTo(draft.Lines));
        Assert.That(Json(drafts.Transcripts), Is.EqualTo(Json([draft])));
        Assert.That(drafts.Samples, Is.Empty);
    }

    /// <summary>
    /// An eval's drafts come back oldest first, samples and transcripts each in their own list, whatever order they
    /// came in.
    /// </summary>
    [Test]
    public async Task AnEvalsDrafts_ComeBackOldestFirst_EachKindInItsList()
    {
        string eval = NewEval();
        var (late, early) = (Sample(eval, 3), Sample(eval, 1));
        var (badLate, badEarly) = (KnownBad(eval, 4, "{}"), KnownBad(eval, 2, "{}"));

        await _store.AddAsync(late);
        await _store.AddAsync(badLate);
        await _store.AddAsync(early);
        await _store.AddAsync(badEarly);

        var drafts = await _store.OfEvalAsync(eval);
        Assert.That(drafts.Samples.Select(d => d.Id), Is.EqualTo(new[] { early.Id, late.Id }));
        Assert.That(drafts.Transcripts.Select(d => d.Id), Is.EqualTo(new[] { badEarly.Id, badLate.Id }));
    }

    /// <summary>An eval only gets its own drafts back, and an eval nobody drafted for gets none.</summary>
    [Test]
    public async Task AnotherEvalsDrafts_AreNotThisEvals()
    {
        string mine = NewEval(), other = NewEval();
        await _store.AddAsync(Sample(mine, 1));
        await _store.AddAsync(Sample(other, 2));
        await _store.AddAsync(KnownBad(other, 3, "{}"));

        var drafts = await _store.OfEvalAsync(mine);
        Assert.That(drafts.Samples.Select(d => d.Eval), Is.EqualTo(new[] { mine }));
        Assert.That(drafts.Transcripts, Is.Empty);
        var none = await _store.OfEvalAsync(NewEval());
        Assert.That((none.Samples, none.Transcripts),
            Is.EqualTo((Array.Empty<SampleDraft>(), Array.Empty<TranscriptDraft>())));
    }

    /// <summary>Each kind is found by its id, and an id of the other kind, or of nothing, finds none.</summary>
    [Test]
    public async Task ADraft_IsFoundByItsId_AsItsOwnKindOnly()
    {
        string eval = NewEval();
        var sample = Sample(eval, 1);
        var transcript = KnownBad(eval, 2, "{}");
        await _store.AddAsync(sample);
        await _store.AddAsync(transcript);

        Assert.That(JsonSerializer.Serialize(await _store.GetSampleAsync(sample.Id)),
            Is.EqualTo(JsonSerializer.Serialize(sample)));
        Assert.That(JsonSerializer.Serialize(await _store.GetTranscriptAsync(transcript.Id)),
            Is.EqualTo(JsonSerializer.Serialize(transcript)));
        Assert.That(await _store.GetSampleAsync(transcript.Id), Is.Null, "a transcript's id finds no sample");
        Assert.That(await _store.GetTranscriptAsync(sample.Id), Is.Null, "a sample's id finds no transcript");
        Assert.That(await _store.GetSampleAsync(NewId()), Is.Null);
    }

    /// <summary>Deleting a draft by its id takes only that one, of either kind, and says it was there.</summary>
    [Test]
    public async Task DeletingADraft_TakesOnlyThatOne_OfEitherKind()
    {
        string eval = NewEval();
        var (gone, kept) = (Sample(eval, 1), Sample(eval, 2));
        var (badGone, badKept) = (KnownBad(eval, 3, "{}"), KnownBad(eval, 4, "{}"));
        foreach (var sample in new[] { gone, kept }) await _store.AddAsync(sample);
        foreach (var transcript in new[] { badGone, badKept }) await _store.AddAsync(transcript);

        Assert.That(await _store.DeleteAsync(gone.Id), Is.True);
        Assert.That(await _store.DeleteAsync(badGone.Id), Is.True);

        var drafts = await _store.OfEvalAsync(eval);
        Assert.That(drafts.Samples.Select(d => d.Id), Is.EqualTo(new[] { kept.Id }));
        Assert.That(drafts.Transcripts.Select(d => d.Id), Is.EqualTo(new[] { badKept.Id }));
    }

    /// <summary>Deleting a draft that isn't there says so.</summary>
    [Test]
    public async Task DeletingADraftThatIsNotThere_SaysSo() =>
        Assert.That(await _store.DeleteAsync(NewId()), Is.False);

    /// <summary>
    /// An eval being composed (F49.5), some seconds after the start: its file's text as the person wrote it, with
    /// Afrikaans and a quote in it, and one rubric.
    /// </summary>
    private static EvalDraft Composed(string name, int seconds, string proves = "Kook \"lekker\" vir vier — ê") =>
        new(NewId(), Identity, name, $$"""{"name":"{{name}}","proves":"{{proves.Replace("\"", "\\\"")}}"}""",
            [new RubricFile("meal.md", "Is dit 'n maaltyd vir vier?\n- ja\n- nee")], "Louis", T0.AddSeconds(seconds));

    /// <summary>An eval draft reads back whole: its file's text exactly, its rubrics, who and when.</summary>
    [Test]
    public async Task AnEvalDraft_ReadsBackWhole()
    {
        var draft = Composed(NewEval(), 1);

        await _store.AddAsync(draft);

        var read = await _store.GetEvalAsync(draft.Id);
        Assert.That(read, Is.Not.Null);
        Assert.That((read!.Identity, read.Name, read.Json, read.Author, read.At),
            Is.EqualTo((draft.Identity, draft.Name, draft.Json, draft.Author, draft.At)));
        Assert.That(read.Rubrics, Is.EqualTo(draft.Rubrics));
    }

    /// <summary>Replacing an eval draft keeps its id and takes the new text; one that isn't there says so.</summary>
    [Test]
    public async Task ReplacingAnEvalDraft_TakesTheNewText_AndOneNotThereSaysSo()
    {
        var draft = Composed(NewEval(), 1);
        await _store.AddAsync(draft);
        var changed = Composed(draft.Name, 5, "Kook vir ses") with { Id = draft.Id };

        Assert.That(await _store.ReplaceAsync(changed), Is.True);
        Assert.That(await _store.ReplaceAsync(Composed(NewEval(), 6)), Is.False, "a new id isn't there to replace");

        var read = await _store.GetEvalAsync(draft.Id);
        Assert.That((read!.Json, read.At), Is.EqualTo((changed.Json, changed.At)));
    }

    /// <summary>
    /// The eval drafts are listed oldest first, and never as another kind: an eval draft is no sample, and a sample's
    /// id finds no eval draft.
    /// </summary>
    [Test]
    public async Task EvalDrafts_AreListedOldestFirst_AndKeptApartFromTheOtherKinds()
    {
        var (later, earlier) = (Composed(NewEval(), 9), Composed(NewEval(), 2));
        var sample = Sample(later.Name, 3);
        await _store.AddAsync(later);
        await _store.AddAsync(earlier);
        await _store.AddAsync(sample);

        var ids = (await _store.EvalsAsync()).Select(d => d.Id).ToList();

        Assert.That(ids.IndexOf(earlier.Id), Is.LessThan(ids.IndexOf(later.Id)));
        Assert.That(ids, Does.Not.Contain(sample.Id));
        Assert.That(await _store.GetEvalAsync(sample.Id), Is.Null);
        Assert.That((await _store.OfEvalAsync(later.Name)).Samples.Select(d => d.Id), Is.EqualTo(new[] { sample.Id }));
    }

    /// <summary>An eval draft is deleted by its id, as the other kinds are.</summary>
    [Test]
    public async Task AnEvalDraft_IsDeletedByItsId()
    {
        var draft = Composed(NewEval(), 1);
        await _store.AddAsync(draft);

        Assert.That(await _store.DeleteAsync(draft.Id), Is.True);
        Assert.That(await _store.GetEvalAsync(draft.Id), Is.Null);
    }
}
