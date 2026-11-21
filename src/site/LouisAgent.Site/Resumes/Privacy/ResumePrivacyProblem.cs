namespace LouisAgent.Site.Resumes.Privacy;

/// <summary>
/// Something a public CV must not hold (F142; POPIA): identity-theft material, or what weakens the next pay talk.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum ResumePrivacyProblem
{
    /// <summary>A South African ID number, or the words for one.</summary>
    IdNumber,

    /// <summary>What a job paid.</summary>
    Salary,

    /// <summary>The date of birth.</summary>
    BirthDate,

    /// <summary>A street address.</summary>
    Address,
}
