namespace LouisAgent.Core.Tests.Hosting.Catalogue;

/// <summary>What the made-up reading tool asks the model for: an account's number and what is due.</summary>
public sealed record SampleAccount(string AccountNumber, decimal TotalDue);
