namespace LouisAgent.Api.IntegrationTests.Sessions;

/// <summary>What the made-up reading tool asks the model for: an account's number and what is due.</summary>
public sealed record AccountReading(string AccountNumber, decimal TotalDue);
