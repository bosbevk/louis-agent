using LouisAgent.Api.Evals.Runs.Kinds;
using LouisAgent.Api.Evals.Store;
using LouisAgent.Api.Evals.Store.Notes;
using LouisAgent.Testing;
using static LouisAgent.Api.Evals.Store.EvalDocuments;
using static LouisAgent.Api.Evals.Runs.Protocol.EvalProtocol;

namespace LouisAgent.Api.Testing;

/// <summary>
/// What every store has to do with notes, in memory and in Mongo alike: keep them in the order written with the time
/// given, keep the run's apart from each conversation's, turn down bad ones, and never lose them when the run is
/// rewritten. Times are whole milliseconds in UTC, as Mongo keeps them.
/// </summary>
/// <remarks>
/// Abstract test (a contract test): the tests are written once, and each store gets a small subclass that only says
/// how to make one. That way the in-memory store the unit tests use and the Mongo store can't drift apart.
/// </remarks>
public abstract class EvalNotesContract
{
    /// <summary>The moment the tests start from.</summary>
    private static readonly DateTimeOffset T0 = new(2026, 10, 27, 9, 0, 0, TimeSpan.Zero);

    /// <summary>The store under test.</summary>
    private IEvalStore _store = null!;

    /// <summary>A store to test. Each test gets a fresh one.</summary>
    protected abstract IEvalStore CreateStore();

    /// <summary>Gives each test its own store.</summary>
    [SetUp]
    public void SetUpStore() => _store = CreateStore();

    /// <summary>A run id nobody else uses, so tests can share a real database.</summary>
    private static string NewId() => Guid.NewGuid().ToString("N")[..12];

    /// <summary>Saves a fresh run and gives back its id.</summary>
    private async Task<string> SavedRunAsync()
    {
        string id = NewId();
        await _store.SaveRunAsync(new Run(id, "check-" + id[..6], 1, EvalRunStatus.Running, T0));
        return id;
    }

    /// <summary>The key of a conversation for a task.</summary>
    private static ConversationKey Conversation(string task) => new(SampleMode.Beta, 1, task);

    /// <summary>Notes come back in the order written, each with its time and its conversation if it has one.</summary>
    [Test]
    public async Task ANoteOnTheRun_AndOnAConversation_ReadBackInTheOrderWritten_EachWithItsTime()
    {
        string id = await SavedRunAsync();
        var bobotie = Conversation("bobotie");

        await _store.AddNoteAsync(id, "judge changed", T0.AddMinutes(1));
        await _store.AddNoteAsync(id, "this one hallucinated a price", T0.AddMinutes(2), bobotie);
        await _store.AddNoteAsync(id, "re-ran after the fix", T0.AddMinutes(3));

        Assert.That(await _store.NotesAsync(id),
            Is.EqualTo(new[]
            {
                new RunNote("judge changed", T0.AddMinutes(1)),
                new RunNote("this one hallucinated a price", T0.AddMinutes(2), bobotie),
                new RunNote("re-ran after the fix", T0.AddMinutes(3)),
            }));
    }

    /// <summary>The run's own notes and each conversation's stay apart.</summary>
    [Test]
    public async Task TheRunsNotes_AndEachConversationsNotes_AreKeptApart()
    {
        string id = await SavedRunAsync();
        await _store.AddNoteAsync(id, "on the run", T0);
        await _store.AddNoteAsync(id, "on bobotie", T0.AddSeconds(1), Conversation("bobotie"));
        await _store.AddNoteAsync(id, "on sosaties", T0.AddSeconds(2), Conversation("sosaties"));

        var notes = await _store.NotesAsync(id);

        Assert.That(notes.Where(n => n.Conversation is null).Select(n => n.Text), Is.EqualTo(new[] { "on the run" }));
        Assert.That(notes.Where(n => n.Conversation == Conversation("bobotie")).Select(n => n.Text),
            Is.EqualTo(new[] { "on bobotie" }));
        Assert.That(notes.Where(n => n.Conversation == Conversation("sosaties")).Select(n => n.Text),
            Is.EqualTo(new[] { "on sosaties" }));
    }

    /// <summary>One run's notes never show up under another run.</summary>
    [Test]
    public async Task ARunsNotes_AreNotAnotherRunsNotes()
    {
        string first = await SavedRunAsync();
        string second = await SavedRunAsync();

        await _store.AddNoteAsync(first, "only the first", T0);

        Assert.That(await _store.NotesAsync(second), Is.Empty);
    }

    /// <summary>A note on a run that isn't there is turned down, and nothing is kept.</summary>
    [Test]
    public async Task ANoteOnARunThatDoesNotExist_IsRefused_AndNothingIsKept()
    {
        string missing = NewId();

        var result = await _store.AddNoteAsync(missing, "hello", T0);

        Assert.That(result, Is.EqualTo(new NoteResult.NoRun()));
        Assert.That(await _store.NotesAsync(missing), Is.Empty);
    }

    /// <summary>A kept note is trimmed.</summary>
    [Test]
    public async Task ANote_IsTrimmed_AndTheResultIsTheNoteAsStored()
    {
        string id = await SavedRunAsync();

        var result = await _store.AddNoteAsync(id, "  \n  spaced out \t ", T0);

        Assert.That(result, Is.EqualTo(new NoteResult.Added(new RunNote("spaced out", T0))));
        Assert.That((await _store.NotesAsync(id)).Single().Text, Is.EqualTo("spaced out"));
    }

    /// <summary>A note with nothing in it is turned down as blank.</summary>
    [TestCase("")]
    [TestCase("   \t\n ")]
    [TestCase(null)]
    public async Task AnEmptyNote_IsRefusedAsBlank(string? text)
    {
        string id = await SavedRunAsync();

        var result = await _store.AddNoteAsync(id, text, T0);

        Assert.That(result, Is.EqualTo(new NoteResult.Refused(NoteRefusal.Blank, 0)));
        Assert.That(await _store.NotesAsync(id), Is.Empty);
    }

    /// <summary>A note right at the limit is kept; one character more is turned down, with its length.</summary>
    [Test]
    public async Task ANoteOverTheLimit_IsRefusedAsTooLong_WithItsLength_AndOneAtTheLimitIsKept()
    {
        string id = await SavedRunAsync();

        var atLimit = await _store.AddNoteAsync(id, new string('a', RunNotes.MaxLength), T0);
        var over = await _store.AddNoteAsync(id, new string('b', RunNotes.MaxLength + 1), T0.AddSeconds(1));

        Assert.That(atLimit, Is.TypeOf<NoteResult.Added>());
        Assert.That(over, Is.EqualTo(new NoteResult.Refused(NoteRefusal.TooLong, RunNotes.MaxLength + 1)));
        Assert.That(await _store.NotesAsync(id), Has.Count.EqualTo(1));
    }

    /// <summary>Spaces around a note don't push it over the limit.</summary>
    [Test]
    public async Task TheLimit_CountsTheTrimmedText()
    {
        string id = await SavedRunAsync();

        var result = await _store.AddNoteAsync(id, "   " + new string('a', RunNotes.MaxLength) + "   ", T0);

        Assert.That(result, Is.TypeOf<NoteResult.Added>());
    }

    /// <summary>The same text twice is two notes.</summary>
    [Test]
    public async Task Notes_AreOnlyAdded_TheSameTextTwiceIsTwoNotes()
    {
        string id = await SavedRunAsync();

        await _store.AddNoteAsync(id, "same", T0);
        await _store.AddNoteAsync(id, "same", T0.AddMinutes(1));

        Assert.That((await _store.NotesAsync(id)).Select(n => n.At), Is.EqualTo(new[] { T0, T0.AddMinutes(1) }));
    }

    /// <summary>
    /// The recorder writes the run again as it grows and when it ends. That must never wipe a note someone wrote
    /// meanwhile.
    /// </summary>
    [Test]
    public async Task TheRecordersRewriteOfTheRun_DoesNotEraseTheNotes()
    {
        string id = await SavedRunAsync();
        await _store.AddNoteAsync(id, "written while it ran", T0);

        await _store.SaveRunAsync(new Run(id, "check-" + id[..6], 1, EvalRunStatus.Done, T0, T0.AddMinutes(5), 0));

        Assert.That((await _store.NotesAsync(id)).Select(n => n.Text), Is.EqualTo(new[] { "written while it ran" }));
    }

    /// <summary>A run with no notes reads as none.</summary>
    [Test]
    public async Task ARunWithNoNotes_ReadsAsNone() =>
        Assert.That(await _store.NotesAsync(await SavedRunAsync()), Is.Empty);

    /// <summary>Deleting a run deletes its notes, so a new run under the same id starts with none.</summary>
    [Test]
    public async Task DeletingTheRun_DeletesItsNotes()
    {
        string id = await SavedRunAsync();
        await _store.AddNoteAsync(id, "gone with the run", T0);

        await _store.DeleteRunAsync(id);
        await _store.SaveRunAsync(new Run(id, "check-" + id[..6], 1, EvalRunStatus.Running, T0));

        Assert.That(await _store.NotesAsync(id), Is.Empty);
    }
}
