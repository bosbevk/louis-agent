using System.Text.Json;

namespace LouisAgent.Site.Resumes.Drafts;

/// <summary>
/// The CV being edited in the admin, beside the one the site last kept. The forms change the draft, the preview
/// draws it, and nothing reaches the site until it is saved.
/// </summary>
/// <remarks>
/// Presentation Model (Fowler), Louis's pick of three for the editor: the screen's state lives here, away from the
/// page, so "is there anything to save?" and "put it back" are tested without a browser, and the forms and the
/// preview read one draft and can't drift apart. Without it each form would keep its own copy of its part.
/// </remarks>
/// <seealso href="https://martinfowler.com/eaaDev/PresentationModel.html">Presentation Model (Fowler)</seealso>
public sealed class ResumeDraft(Resume saved)
{
    /// <summary>The CV as the site last kept it.</summary>
    public Resume Saved { get; private set; } = saved;

    /// <summary>The CV as it is being edited.</summary>
    public Resume Current { get; private set; } = saved;

    /// <summary>
    /// Whether there is anything to save. The two are compared as they would be sent: a record holding a list is
    /// only equal to itself, and an edit that puts the words back should leave nothing to save.
    /// </summary>
    public bool Changed => !ReferenceEquals(Current, Saved) && Written(Current) != Written(Saved);

    /// <summary>Takes an edit: the whole CV with one part changed.</summary>
    public void Edit(Resume edited) => Current = edited;

    /// <summary>Drops the edits, back to what the site holds.</summary>
    public void Discard() => Current = Saved;

    /// <summary>The API kept it, and what it answered is now what the site holds.</summary>
    public void Kept(Resume kept) => (Saved, Current) = (kept, kept);

    /// <summary>The CV as the API reads and writes it.</summary>
    private static string Written(Resume resume) => JsonSerializer.Serialize(resume, ResumeReader.Options);
}
