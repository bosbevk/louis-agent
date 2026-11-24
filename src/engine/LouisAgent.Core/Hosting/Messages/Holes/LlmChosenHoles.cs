using LouisAgent.Core.Config;

namespace LouisAgent.Core.Hosting.Messages.Holes;

/// <summary>The model a host runs on, and whether its tools and thinking are on.</summary>
public sealed record LlmChosenHoles(LlmOptions Llm, string Tools, string Thinking);
