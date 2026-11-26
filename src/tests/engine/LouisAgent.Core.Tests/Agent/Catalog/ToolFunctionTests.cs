using LouisAgent.Core.Agent.Catalog;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

namespace LouisAgent.Core.Tests.Agent.Catalog;

/// <summary>
/// What a tool method becomes for the model is the same bytes on every machine: a description written as a raw string
/// breaks its lines as the checkout does, CRLF on Windows with autocrlf and LF on Linux, and the model is sent LF.
/// </summary>
public class ToolFunctionTests
{
    /// <summary>
    /// A parameter's description reaches the model with LF line endings, as the tool's own does (FX254: CI on Linux and
    /// the goldens made on Windows disagreed on a parameter written as a raw string).
    /// </summary>
    [Test]
    public void EveryDescription_HasLfLineEndings_WhateverTheCheckout()
    {
        var method = typeof(TwoLineTools).GetMethod(nameof(TwoLineTools.Look))!;

        var function = AgentCatalog.FunctionOf(method, _ => new TwoLineTools());
        string schema = function.JsonSchema.GetRawText();

        Assert.Multiple(() =>
        {
            Assert.That(function.Description, Is.EqualTo("Looks.\nTwice."));
            Assert.That(schema, Does.Contain(@"One line.\nAnother."));
            Assert.That(schema, Does.Not.Contain(@"\r"));
        });
    }

    /// <summary>A tool whose descriptions break their lines the Windows way, as a raw string does in a CRLF checkout.</summary>
    private sealed class TwoLineTools
    {
        /// <summary>Gives back what it was asked to look at.</summary>
        [Description("Looks.\r\nTwice.")]
        public string Look([Description("One line.\r\nAnother.")] string what) => what;
    }
}
