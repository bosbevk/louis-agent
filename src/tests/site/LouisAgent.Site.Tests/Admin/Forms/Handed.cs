namespace LouisAgent.Site.Tests.Admin.Forms;

/// <summary>What a form last handed back to its page, for a test to look at.</summary>
internal sealed class Handed<T>
    where T : class
{
    /// <summary>The value as last handed back; nothing until the form does.</summary>
    public T? Value { get; set; }
}
