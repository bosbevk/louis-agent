using System.Text.Json;
using System.Text.RegularExpressions;

namespace LouisAgent.Web.Content;

/// <summary>A web page a turn used or cited: its address, its title when the answer gave one, and its site.</summary>
public sealed record Source(string Url, string Title, string Site);
