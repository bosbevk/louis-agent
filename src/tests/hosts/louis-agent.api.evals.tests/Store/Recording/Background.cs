namespace LouisAgent.Api.Evals.Tests.Evals.Store.Recording;

/// <summary>Waiting for what the recorder does in the background, for the recording's tests.</summary>
internal static class Background
{
    /// <summary>Waits, up to two seconds, until the condition holds; fails the test if it never does.</summary>
    public static async Task Eventually(Func<Task<bool>> condition)
    {
        for (int i = 0; i < 200; i++)
        {
            if (await condition()) return;
            await Task.Delay(10);
        }

        Assert.Fail("the store never got there");
    }
}
