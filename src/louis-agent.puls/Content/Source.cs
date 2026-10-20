using System.Text.Json;
using System.Text.RegularExpressions;
using LouisAgent.Web.Shared;

namespace LouisAgent.Puls.Content;

/// <summary>A web page a turn used or cited (F20): its address, its title when the answer gave one, and its site.</summary>
public sealed record Source(string Url, string Title, string Site);
