namespace LouisAgent.Evals.Grading.Models;

/// <summary>Where the judge runs (ADR 0024: a choice is an enum). The local one is the one this phase uses; the others are on purpose.</summary>
public enum JudgeProvider
{
    Ollama,
    Anthropic,
    OpenAiCompatible,
}
