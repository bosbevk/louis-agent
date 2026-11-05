using LouisAgent.Evals.Runner;
using LouisAgent.PulsEvalRunner;

// Puls's evals: the framework's runner, with the checks and readers they can name.
return await EvalRunnerHost.RunAsync(args, PulsCatalogs.All);
