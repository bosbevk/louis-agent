using Microsoft.Extensions.AI.Evaluation;

namespace LouisAgent.Evals.Grading.Models;

/// <summary>The judge a model grader asks: its client, with its requests recorded in the ledger, and the model's name for the grade.</summary>
public sealed record ConfiguredJudge(ChatConfiguration Chat, string Model);
