using LouisAgent.Core.Agent;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Tests.Agent;

/// <summary>
/// A tool list's mark is the same for the same tools, and changes with anything the model reads of them: a name, a
/// description, the order.
/// </summary>
public class ToolsFingerprintTests
{
    /// <summary>A tool that answers nothing, named and described as given.</summary>
    private static AITool Tool(string name, string description) =>
        AIFunctionFactory.Create(() => "ok", name, description);

    /// <summary>The same tools give the same mark.</summary>
    [Test]
    public void TheSameTools_GiveTheSameMark()
    {
        var once = ToolsFingerprint.Of([Tool("Read", "Reads a file")]);
        var again = ToolsFingerprint.Of([Tool("Read", "Reads a file")]);

        Assert.That(again, Is.EqualTo(once));
    }

    /// <summary>A changed description, a changed name or another order each give another mark.</summary>
    [Test]
    public void WhatTheModelReads_ChangesTheMark()
    {
        var before = ToolsFingerprint.Of([Tool("Read", "Reads a file"), Tool("Write", "Writes a file")]);

        Assert.Multiple(() =>
        {
            Assert.That(ToolsFingerprint.Of([Tool("Read", "Reads a file, pending"), Tool("Write", "Writes a file")]),
                Is.Not.EqualTo(before), "a description");
            Assert.That(ToolsFingerprint.Of([Tool("Load", "Reads a file"), Tool("Write", "Writes a file")]),
                Is.Not.EqualTo(before), "a name");
            Assert.That(ToolsFingerprint.Of([Tool("Write", "Writes a file"), Tool("Read", "Reads a file")]),
                Is.Not.EqualTo(before), "the order");
        });
    }
}
