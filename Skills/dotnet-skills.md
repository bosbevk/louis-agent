# .NET Development Skills

How to build, test, run and debug .NET code with the native `DotNet*` tools. Always use these tools for .NET work instead of guessing whether code compiles or tests pass.

## Tools

| Tool | Use it to |
|------|-----------|
| `DotNetListProjects` | Find the .sln / .csproj files. Call first when you don't know the project path. |
| `DotNetBuild` | Compile. Returns `file(line,col): code: message` for each error/warning. |
| `DotNetTest` | Run tests. Returns the summary plus message and stack trace of each failed test. |
| `DotNetRun` | Run a console app with arguments/stdin. Returns output, exit code and unhandled exception stack traces. |
| `DotNetRestore` / `DotNetClean` | Restore packages / clear stale build output. |
| `DotNetListPackages` | List NuGet packages (`filter`: `outdated` or `vulnerable`). |
| `DotNetAddPackage` / `DotNetRemovePackage` | Change package references. |
| `DotNetSdkInfo` | Check installed SDKs and runtimes (e.g. on `NETSDK1045` "SDK does not support target"). |

All paths are relative to the workspace root; an empty `project` means the workspace root (works when it contains exactly one solution or project).

## Workflows

### Fix a build
1. `DotNetBuild` the solution (or the project you changed).
2. Read each reported file around the given line with `ReadWorkspaceFile` before editing; fix the **first** error first, since later errors are often caused by it.
3. Rebuild. Repeat until it succeeds. Don't stop at "should compile now" — prove it with a build.

### Fix failing tests
1. `DotNetTest` with a `filter` to target the failing tests (`FullyQualifiedName~ClassName` or `FullyQualifiedName~ClassName.MethodName`) — much faster than the whole suite.
2. Use the assertion message and the stack trace's `file:line` to locate both the test and the code under test; read both.
3. Decide whether the code or the test is wrong. Fix the code unless the test's expectation is clearly outdated — say which you chose and why.
4. Re-run the filtered tests, then the full suite once to catch regressions. Use `noBuild: true` only right after a successful `DotNetBuild`.

### Debug a runtime error
1. Reproduce with `DotNetRun` (pass `arguments` / `standardInput` to hit the failing path). Use a short `timeoutSeconds` for apps that wait for input or never exit.
2. The exception type, message and top stack frames in *your* code (not framework frames) point to the cause. Read that code.
3. If the cause is unclear, narrow it down: add temporary `Console.Error.WriteLine` diagnostics or write a small focused test that reproduces it, run again, then remove the diagnostics.
4. Fix, re-run to confirm the error is gone, and run the related tests.

Breakpoint debugging is not available; reproduce → read stack trace → instrument → fix is the loop.

## Reading results

- `FAILED (exit code 1). 0 error(s)` plus raw output → not a compile error: check for a missing SDK (`DotNetSdkInfo`), a wrong path, or a restore failure.
- `MSB3026` / `MSB3027` "file is locked by …" → the app is still running (or attached to a debugger). Tell the user which process holds the lock; don't kill it yourself.
- `NU1101` / `NU1102` → package or version not found; check the id with `DotNetListPackages`.
- `CS0246` / `CS0103` → missing `using`, package or project reference — not a typo in most cases.
- Strange errors after switching between Docker and Windows builds → `DotNetClean` (stale `obj/` from the other OS), then build again.
- A test run that times out usually means a deadlock or a test waiting on input/network; report it rather than retrying.

## Rules

- Verify every code change with `DotNetBuild` (and `DotNetTest` when tests exist) before saying it's done; report the actual result, including remaining warnings you introduced.
- Never claim tests pass without running them. If some fail and you didn't fix them, say which ones.
- Don't add or upgrade NuGet packages unless the task needs it; mention any package change in your answer.
- Don't delete or skip failing tests to make the suite green.
