namespace LouisAgent.Diagrams.Engine.Components.Placed.Lines;

/// <summary>One point a line goes through (F134.7).</summary>
/// <param name="X">How far across.</param>
/// <param name="Y">How far down.</param>
public readonly record struct LinePoint(double X, double Y);
