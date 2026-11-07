using LouisAgent.Core.Skills;
using LouisAgent.Packs.Calculator;

// A test assembly registers the prompts its pack brings before the first is read, as a host's Build does with the
// catalogue's.
[SetUpFixture]
public class CalculatorPrompts
{
    /// <summary>Makes the pack's guide readable to the tests.</summary>
    [OneTimeSetUp]
    public void Register() => SkillFileSource.Use([new CalculatorPack().Prompts]);
}
