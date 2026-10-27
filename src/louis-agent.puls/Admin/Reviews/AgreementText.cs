namespace LouisAgent.Puls.Admin.Reviews;

/// <summary>
/// How one class of the people's grades reads (6a.4c, Louis: "I do not understand what I'm reading"): one plain sentence, what you did and what the judge did with the same answers,
/// and a short share for the bar beside it. The counts stay the API's (<see cref="ClassAgreement"/>); this only words them.
/// </summary>
public static class AgreementText
{
    public static string Sentence(ClassAgreement c)
    {
        string you = c.PersonPassed ? "passed" : "failed";
        if (c.Total == 0) return $"You have {you} no answers yet.";
        string judge = c.PersonPassed ? "passed" : "failed";
        string answers = c.Total == 1 ? "1 answer" : $"{c.Total} answers";
        string did = c.Total == 1
            ? c.Agreed == 1 ? $"The judge {judge} it too." : "The judge did not."
            : c.Agreed == c.Total ? $"The judge {judge} all {c.Total}."
            : c.Agreed == 0 ? $"The judge {judge} none of them."
            : $"The judge {judge} {c.Agreed} of them.";
        return $"You {you} {answers}. {did}";
    }

    public static string Share(ClassAgreement c) => c.Total == 0
        ? "no labels"
        : $"{c.Percent}%" + (c.Few ? " · few labels" : "");

    /// <summary>The bar's width: the share the judge agreed, an empty bar for a class with no labels (6a.5e: one copy for both cards).</summary>
    public static string Width(ClassAgreement c) => $"{c.Percent}%";
}
