using LouisAgent.Testing;
using System.Text.Json;
using LouisAgent.Api.Benchmarks.Runs;
using static LouisAgent.Api.Benchmarks.Runs.BenchmarkProtocol;

namespace LouisAgent.Api.Tests.Benchmarks.Runs;

/// <summary>
/// An eval prints plain lines for people, and an @ followed by one JSON object for the portal. Each line is read into
/// a typed event; anything that can't be read (a plain line, bad JSON, an unknown event, a missing field) becomes a
/// log line, never an exception, so a run never fails on its own output.
/// </summary>
public class BenchmarkProtocolTests
{
    /// <summary>The first line names the protocol's version.</summary>
    [Test]
    public void TheFirstLine_NamesTheProtocolVersion()
    {
        var parsed = Parse("""@{"event":"protocol","version":1}""");

        Assert.That(parsed, Is.EqualTo(new ProtocolStarted(1)));
    }

    /// <summary>The first line names the eval, its version and commit, its graders and its models.</summary>
    [Test]
    public void TheFirstLine_CarriesTheEvalItRuns()
    {
        var started = (ProtocolStarted)Parse(
            """@{"event":"protocol","version":1,"eval":"cook","eval_version":2,"commit":"abc123","graders":[{"name":"priced-with-source","version":1,"kind":"FixedRule"}],"models":[{"mode":"cook","level":"agent","model":"claude-haiku-5-5"}]}""");

        Assert.That((started.Eval, started.EvalVersion, started.Commit), Is.EqualTo(("cook", (int?)2, "abc123")));
        Assert.That(started.Graders!.Single(),
            Is.EqualTo(new GraderInfo("priced-with-source", 1, BenchmarkGraderKind.FixedRule)));
        Assert.That(started.Models!.Single(),
            Is.EqualTo(new ModelInfo("cook", BenchmarkModelLevel.Agent, null, "claude-haiku-5-5")));
    }

    /// <summary>A conversation brings its grades and its outcome.</summary>
    [Test]
    public void AConversation_CarriesItsGradesAndOutcome()
    {
        var ended = (ConversationEnded)Parse(SampleRole.Fill(
            """@{"event":"conversation","mode":"cook","repeat":1,"task":"pannekoek","role":"<RoleAlpha>","scores":{},"seconds":3,"grades":[{"grader":"priced-with-source","version":1,"score":1,"outcome":"Pass","reason":"2 of 2 priced with a source"}],"outcome":"Pass"}"""));

        Assert.That(ended.Outcome, Is.EqualTo(BenchmarkOutcome.Pass));
        Assert.That(ended.Grades!.Single(),
            Is.EqualTo(new GradeInfo("priced-with-source", 1, 1, BenchmarkOutcome.Pass,
                "2 of 2 priced with a source")));
    }

    /// <summary>
    /// A human review's entry on the first line carries the question a person is asked; another grader's has none.
    /// </summary>
    [Test]
    public void AHumanReviewsEntryOnTheFirstLine_CarriesItsQuestion()
    {
        var started = (ProtocolStarted)Parse(SampleEval.Fill(
            """@{"event":"protocol","version":1,"eval":"<EvalBeta>","eval_version":9,"commit":"abc","graders":[{"name":"routed-to","version":1,"kind":"FixedRule"},{"name":"meal-for-people.human","version":1,"kind":"Human","question":"Is this a meal for the people asked for?"}],"models":[]}"""));

        Assert.That(started.Graders![0].Question, Is.Null);
        Assert.That(started.Graders![1].Question, Is.EqualTo("Is this a meal for the people asked for?"));
    }

    /// <summary>
    /// A human review's grade waits as pending until a person gives one: it's kept, and it isn't read as unknown.
    /// </summary>
    [Test]
    public void APendingGrade_IsReadAsPending_NotLostAndNotUnknown()
    {
        var ended = (ConversationEnded)Parse(SampleRole.Fill(
            """@{"event":"conversation","mode":"cook","repeat":1,"task":"pannekoek","role":"<RoleAlpha>","scores":{},"seconds":3,"grades":[{"grader":"meal-for-people.human","version":1,"score":0,"outcome":"Pending","reason":"waits for a person"}],"outcome":"Pass"}"""));

        Assert.That(ended.Grades!.Single().Outcome, Is.EqualTo(BenchmarkOutcome.Pending));
        Assert.That(ended.Outcome, Is.EqualTo(BenchmarkOutcome.Pass), "the trial's outcome is the fixed rules'");
    }

    /// <summary>A model grader's grade names the judge that gave it; a code grader's names none.</summary>
    [Test]
    public void AModelGradersGrade_CarriesTheJudgeThatGaveIt()
    {
        var ended = (ConversationEnded)Parse(SampleRole.Fill(
            """@{"event":"conversation","mode":"cook","repeat":1,"task":"pannekoek","role":"<RoleAlpha>","scores":{},"seconds":3,"grades":[{"grader":"meal-for-people","version":1,"score":1,"outcome":"Pass","reason":"feeds four","judge_model":"qwen3:8b","judge_prompt_hash":"abc123def456"},{"grader":"priced-with-source","version":1,"score":1,"outcome":"Pass","reason":"ok"}],"outcome":"Pass"}"""));

        Assert.That(ended.Grades![0],
            Is.EqualTo(new GradeInfo("meal-for-people", 1, 1, BenchmarkOutcome.Pass, "feeds four", "qwen3:8b",
                "abc123def456")));
        Assert.That(ended.Grades[1].JudgeModel, Is.Null);
    }

    /// <summary>
    /// A conversation that failed because of the infrastructure reads as such. A reader that didn't know the outcome
    /// would drop the whole line.
    /// </summary>
    [Test]
    public void AConversationThatFailedForTheInfrastructure_ReadsAsInfrastructure_NotAsALostLine()
    {
        var ended = (ConversationEnded)Parse(
            """@{"event":"conversation","mode":"cook","repeat":1,"task":"pannekoek","role":null,"scores":{},"seconds":3,"errors":0,"outcome":"Infrastructure"}""");

        Assert.That(ended.Outcome, Is.EqualTo(BenchmarkOutcome.Infrastructure));
    }

    /// <summary>A mode brings its repeat, its toolsets and what its sessions open with.</summary>
    [Test]
    public void AMode_CarriesItsRepeatToolsetsAndTheSessionFieldsItOpensWith()
    {
        var parsed = Parse(SampleToolset.Fill(SampleMode.Fill(
            """@{"event":"mode","mode":"<ModeBeta>","repeat":2,"toolsets":["<Gamma>","<Alpha>"],"role":"puls/voice-af","llm":null,"conversations":4,"t":1760700000.5}""")));

        var mode = (ModeStarted)parsed;
        Assert.That(mode.Mode, Is.EqualTo(SampleMode.Beta));
        Assert.That(mode.Repeat, Is.EqualTo(2));
        Assert.That(mode.Toolsets, Is.EqualTo(new[] { SampleToolset.Gamma, SampleToolset.Alpha }));
        Assert.That(mode.Role, Is.EqualTo("puls/voice-af"));
        Assert.That(mode.Llm, Is.Null);
        Assert.That(mode.Conversations, Is.EqualTo(4));
        Assert.That(mode.At, Is.EqualTo(DateTimeOffset.FromUnixTimeMilliseconds(1_760_700_000_500)));
    }

    /// <summary>An older harness said "personality" where it now says "role", and it still reads as the role.</summary>
    [Test]
    public void AModeFromAHarnessBeforeCL14_SaysPersonality_ReadAsItsRole() =>
        Assert.That(
            ((ModeStarted)Parse(SampleMode.Fill(
                """@{"event":"mode","mode":"<ModeBeta>","repeat":1,"toolsets":["web"],"personality":"puls/voice-af"}""")))
            .Role,
            Is.EqualTo("puls/voice-af"));

    /// <summary>A question brings the session it was asked in.</summary>
    [Test]
    public void AQuestion_CarriesItsSession()
    {
        var parsed =
            Parse(SampleMode.Fill(
                """@{"event":"question","mode":"<ModeBeta>","repeat":1,"task":"bobotie","turn":2,"session":"sess_abc","text":"En vir 6?"}"""));

        Assert.That(parsed,
            Is.EqualTo(new QuestionAsked(new TurnKey(SampleMode.Beta, 1, "bobotie", 2), "sess_abc", "En vir 6?",
                null)));
    }

    /// <summary>A streamed event keeps its data as JSON.</summary>
    [Test]
    public void AnSseEvent_KeepsItsDataAsJson()
    {
        var parsed = (SseEvent)Parse(SampleMode.Fill(
            """@{"event":"sse","mode":"<ModeAlpha>","repeat":1,"task":"bobotie","turn":1,"type":"content_block_delta","data":{"delta":{"type":"text_delta","text":"Hallo"}}}"""));

        Assert.That(parsed.Key, Is.EqualTo(new TurnKey(SampleMode.Alpha, 1, "bobotie", 1)));
        Assert.That(parsed.Type, Is.EqualTo("content_block_delta"));
        Assert.That(parsed.Data.GetProperty("delta").GetProperty("text").GetString(), Is.EqualTo("Hallo"));
    }

    /// <summary>A turn brings its session, its calls, the sites it read and its answer.</summary>
    [Test]
    public void ATurn_CarriesItsSessionCallsHostsAndAnswer()
    {
        var turn = (TurnEnded)Parse(SampleMode.Fill(SampleRole.Fill(
            """@{"event":"turn","mode":"<ModeBeta>","repeat":1,"task":"bobotie","turn":1,"session":"sess_abc","question":"Bobotie vir 4","answer":"Hier is…","role":"<RoleAlpha>","continued":null,"puls_calls":1,"specialist_calls":7,"hosts":["www.checkers.co.za"],"seconds":41.3,"error":null}""")));

        Assert.That(turn.Key, Is.EqualTo(new TurnKey(SampleMode.Beta, 1, "bobotie", 1)));
        Assert.That(turn.Session, Is.EqualTo("sess_abc"));
        Assert.That((turn.Question, turn.Answer, turn.Role, turn.Continued),
            Is.EqualTo(("Bobotie vir 4", "Hier is…", SampleRole.Alpha, (string?)null)));
        Assert.That((turn.PulsCalls, turn.SpecialistCalls, turn.Seconds), Is.EqualTo((1, 7, 41.3)));
        Assert.That(turn.Hosts, Is.EqualTo(new[] { "www.checkers.co.za" }));
        Assert.That(turn.Error, Is.Null);
    }

    /// <summary>
    /// A turn's failure is the infrastructure's, not the agent's, and is read by its snake-case name. Without it a
    /// stored run would blame the agent.
    /// </summary>
    [TestCase("timeout", BenchmarkTurnFailure.Timeout)]
    [TestCase("transport", BenchmarkTurnFailure.Transport)]
    [TestCase("api_error", BenchmarkTurnFailure.ApiError)]
    [TestCase("replay_miss", BenchmarkTurnFailure.ReplayMiss)]
    public void ATurnsFailure_IsTheInfrastructuresNotTheAgents_ReadByItsSnakeCaseName(string name,
        BenchmarkTurnFailure expected)
    {
        var line = SampleMode
            .Fill(
                """@{"event":"turn","mode":"<ModeBeta>","repeat":1,"task":"bobotie","turn":1,"session":"s","question":"q","answer":"","role":null,"seconds":1.5,"failure":"NAME"}""")
            .Replace("NAME", name);

        Assert.That(((TurnEnded)Parse(line)).Failure, Is.EqualTo(expected));
    }

    /// <summary>The first line names the split the run asked for.</summary>
    [TestCase("dev", BenchmarkRunSplit.Dev)]
    [TestCase("held_out", BenchmarkRunSplit.HeldOut)]
    [TestCase("all", BenchmarkRunSplit.All)]
    public void TheFirstLine_NamesTheSplitTheRunAskedFor(string wire, BenchmarkRunSplit expected) =>
        Assert.That(
            ((ProtocolStarted)Parse(SampleEval
                .Fill("""@{"event":"protocol","version":1,"eval":"<EvalBeta>","split":"WIRE"}""")
                .Replace("WIRE", wire))).Split, Is.EqualTo(expected));

    /// <summary>A first line with no split has none; one naming a split we don't know can't be read.</summary>
    [Test]
    public void AFirstLineWithNoSplit_HasNone_AndOneThatNamesNoSplitIsAnUnreadableLine()
    {
        Assert.That(
            ((ProtocolStarted)Parse(SampleEval.Fill("""@{"event":"protocol","version":1,"eval":"<EvalBeta>"}""")))
            .Split, Is.Null);
        Assert.That(Parse(SampleEval.Fill("""@{"event":"protocol","version":1,"eval":"<EvalBeta>","split":"some"}""")),
            Is.InstanceOf<LogLine>());
    }

    /// <summary>A turn with no failure has none; one naming a failure we don't know can't be read.</summary>
    [Test]
    public void ATurnWithNoFailureField_HasNone_AndOneThatNamesNoFailureIsAnUnreadableLine()
    {
        Assert.That(
            ((TurnEnded)Parse(
                """@{"event":"turn","mode":"m","repeat":1,"task":"t","turn":1,"session":"s","question":"q","seconds":1}"""))
            .Failure, Is.Null);
        Assert.That(
            Parse(
                """@{"event":"turn","mode":"m","repeat":1,"task":"t","turn":1,"session":"s","question":"q","seconds":1,"failure":"gremlins"}"""),
            Is.InstanceOf<LogLine>());
    }

    /// <summary>
    /// A conversation keeps the eval's scores just as they came, since each eval scores its own way.
    /// </summary>
    [Test]
    public void AConversation_KeepsTheChecksScoresAsTheyCame()
    {
        var conversation = (ConversationEnded)Parse(SampleMode.Fill(SampleRole.Fill(
            """@{"event":"conversation","mode":"<ModeBeta>","repeat":1,"task":"bobotie","role":"<RoleAlpha>","scores":{"routed_right":true,"priced_ratio":0.75},"errors":0,"seconds":88.1}""")));

        Assert.That(conversation.Key, Is.EqualTo(new ConversationKey(SampleMode.Beta, 1, "bobotie")));
        Assert.That(conversation.Role, Is.EqualTo(SampleRole.Alpha));
        Assert.That(conversation.Scores.GetProperty("priced_ratio").GetDouble(), Is.EqualTo(0.75));
        Assert.That((conversation.Errors, conversation.Seconds), Is.EqualTo((0, 88.1)));
    }

    /// <summary>A mode's end only marks the end; any cost on it is ignored, since costs come from the ledger.</summary>
    [Test]
    public void AModesEnd_IsAMarkerWithNoCost()
    {
        Assert.That(Parse(SampleMode.Fill("""@{"event":"mode_done","mode":"<ModeBeta>","repeat":2,"cost":0.42}""")),
            Is.EqualTo(new ModeEnded(SampleMode.Beta, 2)));
    }

    /// <summary>Any other line, broken or unknown, is a log line and never an error.</summary>
    [TestCase("puls-cook  bobotie  role=puls-cook routed=True")]
    [TestCase("")]
    [TestCase("@")]
    [TestCase("@not json")]
    [TestCase("@[1,2]")]
    [TestCase("""@{"event":"mystery","mode":"x"}""")]
    [TestCase("""@{"mode":"mode-beta"}""")]
    [TestCase("""@{"event":"question","mode":"mode-beta","repeat":1,"task":"bobotie","turn":1,"text":"no session"}""")]
    [TestCase(
        """@{"event":"turn","mode":"mode-beta","task":"bobotie","turn":1,"session":"s","question":"no repeat"}""")]
    [TestCase("""@{"event":"mode","mode":"mode-beta","repeat":"two","toolsets":[]}""")]
    [TestCase("""@{"event":"protocol"}""")]
    public void AnythingElse_IsALogLine_NeverAnError(string line)
    {
        Assert.That(Parse(line), Is.EqualTo(new LogLine(line)));
    }

    /// <summary>
    /// What the runner wrote to stderr is merged into its lines and stays as it came, a traceback included.
    /// </summary>
    [Test]
    public void ALogLine_FromTheScriptsStderr_StaysAsItCame()
    {
        const string traceback = "Traceback (most recent call last):";

        Assert.That(Parse(traceback), Is.EqualTo(new LogLine(traceback)));
    }

    /// <summary>
    /// A streamed event's data outlives its line: events are kept until the turn ends, long after the JSON they came
    /// from would have been thrown away.
    /// </summary>
    [Test]
    public void TheSseDataAndTheScores_OutliveTheLine()
    {
        var sse = (SseEvent)Parse(
            """@{"event":"sse","mode":"m","repeat":1,"task":"t","turn":1,"type":"message_start","data":{"id":"msg_1"}}""");
        // Collect now, so data that leaned on a disposed document would show.
        GC.Collect();

        Assert.That(sse.Data.ValueKind, Is.EqualTo(JsonValueKind.Object));
        Assert.That(sse.Data.GetProperty("id").GetString(), Is.EqualTo("msg_1"));
    }
}
