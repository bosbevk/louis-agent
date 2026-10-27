namespace LouisAgent.Puls.Admin.Reviews;

/// <summary>
/// The longest note a review may carry (F18 step 6a.3c), beside <see cref="ReviewerName.MaxLength"/>: the portal stops the typing at the limit the API holds a note to, so it never sends a grade the
/// API would refuse. The API's is <c>ReviewEndpoints.MaxNoteLength</c> (2,000); the two projects don't reference each other, so change both.
/// </summary>
public static class ReviewNote
{
    public const int MaxLength = 2000;
}
