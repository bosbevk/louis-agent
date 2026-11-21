using LouisAgent.Evals.Runner;
using LouisAgent.Evals.Runner.Catalog;

// The framework's runner with the general checks alone: the evals it lists are the stub and the template beside it.
return await EvalRunnerHost.RunAsync(args, RunnerCatalogs.GeneralOnly);
