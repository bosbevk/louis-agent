using System.Text.Json;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Core.Tests.Logging;

// CL3 step 1: ILogger with Serilog behind it. stdout carries the ACP and MCP protocols, so nothing may log there.
[NonParallelizable]
public class AgentLoggingTests
{
    private TextWriter _out = null!, _error = null!;
    private StringWriter _stdout = null!, _stderr = null!;
    private string _directory = null!;

    [SetUp]
    public void SetUp()
    {
        (_out, _error) = (Console.Out, Console.Error);
        Console.SetOut(_stdout = new StringWriter());
        Console.SetError(_stderr = new StringWriter());
        _directory = Directory.CreateTempSubdirectory("logging-").FullName;
    }

    [TearDown]
    public void TearDown()
    {
        AgentLogging.Configure(null);   // flushes and closes the files
        Console.SetOut(_out);
        Console.SetError(_error);
        Directory.Delete(_directory, recursive: true);
    }

    [Test]
    public void ALogLine_GoesToStderr_AsBefore_AndNeverToStdout()
    {
        AgentLogging.Configure(null);

        AgentLogging.For<AgentLoggingTests>().LogWarning("Rates from {Url} unavailable", "https://example.com");

        Assert.That(_stderr.ToString(), Does.Contain("[WARN] Rates from https://example.com unavailable"), "values read as plain text, as today");
        Assert.That(_stdout.ToString(), Is.Empty);
    }

    [Test]
    public void ALogMessage_BindsValuesByHoleName_NotByOrder()
    {
        AgentLogging.Configure(null);

        AgentLogging.For<AgentLoggingTests>().Log(LogLevel.Information, LogMessage.TruncatedToolCall,
            new { Arguments = "{}", MaxOutputTokens = 16000, Tool = "WriteWorkspaceFile" });

        Assert.That(_stderr.ToString(),
            Does.Contain("[INFO] WriteWorkspaceFile was cut off by the output limit (16000 tokens); arguments {}"));
    }

    [Test]
    public void TheBrackets_ReadAsTheyAlwaysHave_OrCarryTheCallsTag()
    {
        AgentLogging.Configure(null);
        var logger = AgentLogging.For<AgentLoggingTests>();

        logger.Log(LogLevel.Error, LogMessage.SkillFileLoadFailed, new { File = "devops-skills.md", Reason = "locked" });
        logger.Log(LogLevel.Information, LogMessage.McpToolsDiscovered, new { Count = 3 }, tag: "MCP");

        Assert.That(_stderr.ToString(), Does.Contain("[ERROR] Failed to load devops-skills.md: locked"), "not Serilog's EROR");
        Assert.That(_stderr.ToString(), Does.Contain("[MCP] Discovered 3 tools from Rider"));
    }

    [Test]
    public void ALogMessage_MissingAValue_Throws()
    {
        Assert.That(() => AgentLogging.For<AgentLoggingTests>().Log(LogLevel.Information, LogMessage.TruncatedToolCall, new { Tool = "x" }),
            Throws.ArgumentException.With.Message.Contain("LogMessage.TruncatedToolCall needs a value for '{MaxOutputTokens}'"));
    }

    [Test]
    public void ARecord_GoesToItsJsonLinesFile_NotToTheConsole()
    {
        AgentLogging.Configure(_directory);

        ToolCallRecords.TruncatedToolCall("WriteWorkspaceFile", "{\"path\":\"a.cs\"}", 16000);
        AgentLogging.Configure(null);   // flush

        string[] lines = File.ReadAllLines(Path.Combine(_directory, "truncated-tool-calls.jsonl"));
        Assert.That(lines, Has.Length.EqualTo(1));
        using var record = JsonDocument.Parse(lines[0]);
        Assert.That(record.RootElement.GetProperty("tool").GetString(), Is.EqualTo("WriteWorkspaceFile"));
        Assert.That(record.RootElement.GetProperty("maxOutputTokens").GetInt32(), Is.EqualTo(16000));
        Assert.That(_stderr.ToString(), Does.Not.Contain("cut off"), "records stay out of the console");
        // Each kind has its own file (Serilog opens it when configured, so it exists, empty).
        Assert.That(File.ReadAllText(Path.Combine(_directory, "oversized-tool-results.jsonl")), Is.Empty);
    }

    [Test]
    public void ARecord_KeepsTheShapeTheFilesAlwaysHad_NothingOfSerilogs()
    {
        // F16's report reads oversized-tool-results.jsonl: the keys AgentLog wrote before CL3, camelCase, and no @mt,
        // Tag, SourceContext or Record marker.
        AgentLogging.Configure(_directory);

        ToolCallRecords.OversizedToolResult("GetDiff", "{}", 120_000, "review", "summarised", 900);
        ToolCallRecords.OversizedToolResult("ReadWorkspaceFile", "{}", 60_000, null, "truncated", 50_000);
        AgentLogging.Configure(null);   // flush

        string[] lines = File.ReadAllLines(Path.Combine(_directory, "oversized-tool-results.jsonl"));
        using var record = JsonDocument.Parse(lines[0]);
        Assert.That(record.RootElement.EnumerateObject().Select(p => p.Name).Order(),
            Is.EqualTo(new[] { "arguments", "chars", "outcome", "returnedChars", "timestamp", "tool", "userRequest" }));
        Assert.That(DateTimeOffset.TryParse(record.RootElement.GetProperty("timestamp").GetString(), out _), Is.True);
        Assert.That(record.RootElement.GetProperty("chars").GetInt32(), Is.EqualTo(120_000), "numbers stay numbers");
        Assert.That(lines[1], Does.Contain("\"userRequest\":null"));
    }

    [Test]
    public void ALoggerMadeBeforeConfigure_StillWritesWhereConfigureSays()
    {
        // Static loggers are made when their type is first used, often before the host configures logging.
        ILogger early = AgentLogging.For<AgentLoggingTests>();
        AgentLogging.Configure(_directory);

        using (early.BeginScope(new Dictionary<string, object> { [AgentLogging.RecordProperty] = "truncated-tool-call" }))
            early.Log(LogLevel.Information, LogMessage.TruncatedToolCall, new { Tool = "Early", MaxOutputTokens = 1, Arguments = "{}" });
        AgentLogging.Configure(null);   // flush

        Assert.That(File.ReadAllLines(Path.Combine(_directory, "truncated-tool-calls.jsonl")), Has.Length.EqualTo(1));
    }

    [Test]
    public void WithALogDirectory_TheRunFileCopiesTheConsole_WithTheTime_AndKeepsItsFileWhenConfiguredAgain()
    {
        AgentLogging.Configure(_directory);
        string runFile = AgentLogging.RunFile!;
        AgentLogging.For<AgentLoggingTests>().LogWarning("Rates from {Url} unavailable", new Uri("https://example.com/rates"));
        ToolCallRecords.TruncatedToolCall("WriteWorkspaceFile", "{}", 16000);
        AgentLogging.Configure(_directory);   // AgentHost may configure twice: same file, announced once
        AgentLogging.For<AgentLoggingTests>().LogInformation("Second configuration");
        AgentLogging.Configure(null);   // flush

        Assert.That(Path.GetFileName(runFile), Does.Match(@"^agent-.+-\d{8}-\d{6}-\d+\.log$"));
        string[] lines = File.ReadAllLines(runFile);
        Assert.That(lines, Has.Length.EqualTo(3), string.Join("\n", lines));
        Assert.That(lines[0], Does.Match(@"^\d\d:\d\d:\d\d\.\d{3} \[INFO\] Logging to .+\.log$"));
        Assert.That(lines[1], Does.Match(@"^\d\d:\d\d:\d\d\.\d{3} \[WARN\] Rates from https://example.com/rates unavailable$"),
            "values read as plain text, as on the console: no JSON quotes around a Uri");
        Assert.That(lines[2], Does.EndWith("[INFO] Second configuration"));
        Assert.That(_stderr.ToString(), Does.Contain("[INFO] Logging to " + runFile));
        Assert.That(_stdout.ToString(), Is.Empty);
    }

    [Test]
    public void AnUnusableLogDirectory_WarnsAndKeepsTheConsole()
    {
        string file = Path.Combine(_directory, "not-a-directory");
        File.WriteAllText(file, "");

        AgentLogging.Configure(file);

        Assert.That(AgentLogging.RunFile, Is.Null);
        Assert.That(_stderr.ToString(), Does.Contain($"[WARN] File logging disabled; could not use '{file}': "));
    }

    [Test]
    public void WithoutALogDirectory_RecordsAreDropped()
    {
        AgentLogging.Configure(null);

        ToolCallRecords.OversizedToolResult("GetDiff", "{}", 120_000, "review", "summarised", 900);

        Assert.That(_stderr.ToString(), Is.Empty);
        Assert.That(Directory.EnumerateFiles(_directory), Is.Empty);
    }
}