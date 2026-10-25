namespace LouisAgent.Puls.Admin.Grades;

/// <summary>One chip of a conversation's grades: its text, its <c>.chip</c> kind (<c>ok</c>, <c>bad</c>, <c>warn</c>, or none) and the reason as its title.</summary>
public sealed record GradeChip(string Text, string Kind = "", string? Title = null);
