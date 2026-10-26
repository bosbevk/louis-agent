using LouisAgent.Puls.Admin.Reviews;
using Microsoft.JSInterop;

namespace LouisAgent.Puls.Tests.Admin.Reviews;

// F18 step 6a.3: the portal's gate is one shared key, so there is no logged-in person: the portal asks for a reviewer's name once and keeps it in the browser (try/catch: storage may be
// blocked, and the page must work without it), and remembers which conversations this session has graded, so the judge's chip is shown again as soon as a grade is given.
// Written first: ReviewerState and ReviewerName did not exist.
public class ReviewerStateTests
{
    private static readonly ConversationRef Bobotie = new("run1", "puls-cook", 1, "bobotie");

    private sealed class BlockedStorage : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => throw new JSException("storage is blocked");
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => throw new JSException("storage is blocked");
    }

    [TestCase("Louis", "Louis")]
    [TestCase("  Louis  ", "Louis")]
    public void ANameIsTrimmed(string typed, string expected) => Assert.That(ReviewerName.Clean(typed), Is.EqualTo(expected));

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void ABlankNameIsNoName(string? typed) => Assert.That(ReviewerName.Clean(typed), Is.Null);

    [Test]
    public void ANameOver100Characters_IsNoName_TheAPIWouldRefuseIt() =>
        Assert.That(ReviewerName.Clean(new string('L', ReviewerName.MaxLength + 1)), Is.Null);

    [Test]
    public async Task TheNameIsRead_OnceFromTheBrowsersStorage()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Setup<string?>("localStorage.getItem", ReviewerState.StorageKey).SetResult("Louis");
        var state = new ReviewerState(ctx.JSInterop.JSRuntime);

        await state.LoadAsync();
        await state.LoadAsync();

        Assert.That(state.Name, Is.EqualTo("Louis"));
        Assert.That(ctx.JSInterop.Invocations.Count(i => i.Identifier == "localStorage.getItem"), Is.EqualTo(1));
    }

    [Test]
    public async Task ANameThatIsSet_IsKept_AndTellsThePageItChanged()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.SetupVoid("localStorage.setItem", _ => true).SetVoidResult();
        var state = new ReviewerState(ctx.JSInterop.JSRuntime);
        int changes = 0;
        state.Changed += () => changes++;

        bool accepted = await state.SetAsync("  Louis ");

        Assert.That((accepted, state.Name, changes), Is.EqualTo((true, "Louis", 1)));
        Assert.That(ctx.JSInterop.Invocations.Single(i => i.Identifier == "localStorage.setItem").Arguments, Is.EqualTo(new object?[] { ReviewerState.StorageKey, "Louis" }));
    }

    // 6a.4 (Louis, "not you?"): a kept name can be let go, so another person can grade in this browser; the page is told, so the panels ask for a name again.
    [Test]
    public async Task AKeptName_CanBeCleared_AndThePageIsTold()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.SetupVoid("localStorage.setItem", _ => true).SetVoidResult();
        ctx.JSInterop.SetupVoid("localStorage.removeItem", ReviewerState.StorageKey).SetVoidResult();
        var state = new ReviewerState(ctx.JSInterop.JSRuntime);
        await state.SetAsync("Test");
        int changes = 0;
        state.Changed += () => changes++;

        await state.ClearAsync();

        Assert.That(state.Name, Is.Null);
        Assert.That(changes, Is.EqualTo(1));
        Assert.That(ctx.JSInterop.Invocations.Any(i => i.Identifier == "localStorage.removeItem"), Is.True);
    }

    [Test]
    public async Task ABlankNameIsNotKept()
    {
        using var ctx = new BunitContext();
        var state = new ReviewerState(ctx.JSInterop.JSRuntime);

        Assert.That(await state.SetAsync("   "), Is.False);
        Assert.That(state.Name, Is.Null);
    }

    [Test]
    public async Task WithStorageBlocked_NothingBreaks_TheNameStillWorksForThisPageOnly()
    {
        var state = new ReviewerState(new BlockedStorage());

        await state.LoadAsync();
        bool accepted = await state.SetAsync("Louis");

        Assert.That((accepted, state.Name), Is.EqualTo((true, "Louis")), "the person can grade; the name is just not remembered");
    }

    [Test]
    public void AGradeGivenThisSession_IsRemembered_PerConversationAndJudge()
    {
        using var ctx = new BunitContext();
        var state = new ReviewerState(ctx.JSInterop.JSRuntime);
        int changes = 0;
        state.Changed += () => changes++;

        state.Record(Bobotie, "meal-for-people");

        Assert.That(state.Graded(Bobotie, "meal-for-people"), Is.True);
        Assert.That(state.Graded(Bobotie, "another-judge"), Is.False);
        Assert.That(state.Graded(Bobotie with { Task = "sosaties" }, "meal-for-people"), Is.False);
        Assert.That(state.Graded(Bobotie with { Run = "run2" }, "meal-for-people"), Is.False);
        Assert.That(changes, Is.EqualTo(1));
    }
}
