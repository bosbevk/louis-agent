using LouisAgent.Core.Config;

namespace LouisAgent.Core.Tests;

// F14 step 5: usage-probe --host-culture writes HOST_CULTURE into config/.env; only that line may change.
public class EnvFileTests
{
    private const string Comment = "The PC's culture (F14).";

    private static string Set(string content, string value = "en-ZA") =>
        EnvFile.SetValue(content, "HOST_CULTURE", value, Comment, out _);

    [Test]
    public void ExistingLine_IsReplaced_EverythingElseKept()
    {
        const string env = "# LLM\nLLM_PROFILE=ollama\nHOST_CULTURE=en-GB\nAGENT_FUNCTION=louis\n";

        string updated = EnvFile.SetValue(env, "HOST_CULTURE", "en-ZA", Comment, out bool replaced);

        Assert.That(replaced, Is.True);
        Assert.That(updated, Is.EqualTo("# LLM\nLLM_PROFILE=ollama\nHOST_CULTURE=en-ZA\nAGENT_FUNCTION=louis\n"));
    }

    [Test]
    public void Missing_IsAppendedWithItsComment()
    {
        string updated = EnvFile.SetValue("LLM_PROFILE=ollama\n", "HOST_CULTURE", "en-ZA", Comment, out bool replaced);

        Assert.That(replaced, Is.False);
        Assert.That(updated, Is.EqualTo("LLM_PROFILE=ollama\n\n# The PC's culture (F14).\nHOST_CULTURE=en-ZA\n"));
    }

    [Test]
    public void NoFinalNewline_StillAppendsOnItsOwnLine()
    {
        Assert.That(Set("LLM_PROFILE=ollama"), Is.EqualTo("LLM_PROFILE=ollama\n\n# The PC's culture (F14).\nHOST_CULTURE=en-ZA\n"));
    }

    [Test]
    public void CrlfFile_KeepsCrlf()
    {
        // config/.env on a Windows checkout has CRLF endings; the new lines must match.
        Assert.That(Set("LLM_PROFILE=ollama\r\nHOST_CULTURE=en-GB\r\nX=1\r\n"), Is.EqualTo("LLM_PROFILE=ollama\r\nHOST_CULTURE=en-ZA\r\nX=1\r\n"));
        Assert.That(Set("LLM_PROFILE=ollama\r\n"), Is.EqualTo("LLM_PROFILE=ollama\r\n\r\n# The PC's culture (F14).\r\nHOST_CULTURE=en-ZA\r\n"));
    }

    [Test]
    public void CommentedExample_IsLeftAlone()
    {
        // config/.env.example style: "# HOST_CULTURE=en-ZA" is documentation, not the setting.
        string updated = Set("# HOST_CULTURE=en-US\nLLM_PROFILE=ollama\n");

        Assert.That(updated, Does.StartWith("# HOST_CULTURE=en-US\nLLM_PROFILE=ollama\n"));
        Assert.That(updated, Does.EndWith("\nHOST_CULTURE=en-ZA\n"));
    }

    [Test]
    public void ACommentRepeatingTheLine_IsNotTheOneChanged()
    {
        Assert.That(Set("# HOST_CULTURE=en-GB\nHOST_CULTURE=en-GB\n"), Is.EqualTo("# HOST_CULTURE=en-GB\nHOST_CULTURE=en-ZA\n"));
    }

    [Test]
    public void ASettingThatOnlyStartsTheSame_IsNotReplaced()
    {
        string updated = Set("HOST_CULTURE_OLD=x\n");

        Assert.That(updated, Does.StartWith("HOST_CULTURE_OLD=x\n").And.EndWith("\nHOST_CULTURE=en-ZA\n"));
    }

    [Test]
    public void EmptyFile_GetsTheSetting()
    {
        Assert.That(Set(""), Is.EqualTo("\n# The PC's culture (F14).\nHOST_CULTURE=en-ZA\n"));
    }

    [Test]
    public void BadKeyOrMultiLineValue_Throw()
    {
        Assert.That(() => EnvFile.SetValue("", "host culture", "x", Comment, out _), Throws.ArgumentException);
        Assert.That(() => EnvFile.SetValue("", "HOST_CULTURE", "en-ZA\nEVIL=1", Comment, out _), Throws.ArgumentException);
    }

    [Test]
    public void Write_UpdatesTheFileOnDisk()
    {
        string dir = Directory.CreateTempSubdirectory("env-file-").FullName;
        try
        {
            Directory.CreateDirectory(Path.Combine(dir, "config"));
            string env = Path.Combine(dir, "config", ".env");
            File.WriteAllText(env, "LLM_PROFILE=ollama\n");

            Assert.That(EnvFile.Find(Path.Combine(dir, "config")), Is.EqualTo(env));
            Assert.That(EnvFile.Write(env, "HOST_CULTURE", "en-ZA", Comment), Is.False);
            Assert.That(EnvFile.Write(env, "HOST_CULTURE", "en-GB", Comment), Is.True);
            Assert.That(File.ReadAllText(env), Does.EndWith("\nHOST_CULTURE=en-GB\n").And.Not.Contain("en-ZA"));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}