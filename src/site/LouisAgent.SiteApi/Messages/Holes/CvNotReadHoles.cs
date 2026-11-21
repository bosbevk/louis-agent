namespace LouisAgent.SiteApi.Messages.Holes;

/// <summary>Why a saved CV couldn't be read: the reader's own words, with the field and the line.</summary>
public sealed record CvNotReadHoles(string Reason);
