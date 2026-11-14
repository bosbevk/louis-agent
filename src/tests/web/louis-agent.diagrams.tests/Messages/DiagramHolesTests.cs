using System.Reflection;
using System.Runtime.CompilerServices;
using LouisAgent.Diagrams.Messages;
using LouisAgent.Diagrams.Sources;

namespace LouisAgent.Diagrams.Tests.Messages;

/// <summary>
/// F134.7: what the reader hands a message to fill its holes is a named record, declared and then passed (ADR 0119),
/// where it was an unnamed object written inside the call. A record is named for its message, and its properties
/// are that message's holes, so a hole can't be misspelt into a message that still says "{Id}".
/// </summary>
public class DiagramHolesTests
{
    /// <summary>The words between braces in a message's template, such as <c>Id</c> in "names '{Id}'".</summary>
    private static List<string> HolesOf(DiagramMessage message)
    {
        string template = typeof(DiagramMessage).GetField(message.ToString())!
            .GetCustomAttribute<System.ComponentModel.DescriptionAttribute>()!.Description;

        // Every odd piece, cut at an opening brace, starts with a hole's name and its closing brace.
        return [.. template.Split('{').Skip(1).Select(piece => piece[..piece.IndexOf('}')]).Distinct().Order()];
    }

    /// <summary>
    /// The record named for a message, wherever under the messages' holes it is filed. None when there is none.
    /// </summary>
    private static Type? RecordOf(DiagramMessage message) => typeof(DiagramReader).Assembly.GetTypes()
        .FirstOrDefault(type => type.Name == $"{message}Holes" &&
                                type.Namespace?.StartsWith("LouisAgent.Diagrams.Messages.Holes") == true);

    /// <summary>
    /// Every message the reader fills has its record, and the record's properties are the message's holes, no more
    /// and no fewer.
    /// </summary>
    [TestCase(DiagramMessage.Unreadable)]
    [TestCase(DiagramMessage.UnknownKind)]
    [TestCase(DiagramMessage.MissingPart)]
    [TestCase(DiagramMessage.UnknownBox)]
    [TestCase(DiagramMessage.UnknownGroup)]
    [TestCase(DiagramMessage.UnknownCard)]
    [TestCase(DiagramMessage.UnknownParticipant)]
    [TestCase(DiagramMessage.NoteOverNoOne)]
    [TestCase(DiagramMessage.EmptyBlock)]
    [TestCase(DiagramMessage.UnknownType)]
    [TestCase(DiagramMessage.TicketRowCount)]
    [TestCase(DiagramMessage.TicketHalfRow)]
    public void AMessageTheReaderFills_HasItsRecord_WithItsHoles(DiagramMessage message)
    {
        var record = RecordOf(message);

        Assert.That(record, Is.Not.Null, $"no {message}Holes record");
        Assert.That(record!.GetProperties().Select(property => property.Name).Order(), Is.EqualTo(HolesOf(message)));
    }

    /// <summary>The repo's root, found from this file: the first folder above it that holds the solution.</summary>
    private static string RepoRoot([CallerFilePath] string here = "")
    {
        string folder = Path.GetDirectoryName(here)!;
        while (Directory.GetFiles(folder, "*.sln").Length == 0) folder = Path.GetDirectoryName(folder)!;
        return folder;
    }

    /// <summary>The reader builds no unnamed object: each one is a record, a local named for what it is.</summary>
    [Test]
    public void TheReader_BuildsNoUnnamedObject()
    {
        string reader = File.ReadAllText(Path.Combine(RepoRoot(),
            "src/web/louis-agent.diagrams/Sources/DiagramReader.cs"));

        Assert.That(reader, Does.Not.Contain("new {").And.Not.Contain("new\n").And.Not.Contain("new\r\n"));
    }
}
