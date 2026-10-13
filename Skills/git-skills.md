# Git Skills

Guidance for the `git` toolset: status, diffs, branches, staging, commits, merges and history.

## Rules

1. **Inspect before you commit.** `GetStatus`, `DiffStaged`, `DiffUnstaged`. No surprises.
2. **Check for uncommitted changes** before switching branches.
3. **Push smart.** `--force-with-lease` is built in. Don't overwrite someone else's work.
4. **Commit messages explain why.** The what is in the diff.
5. **Branch for features.** Don't commit to main directly.
6. **Fetch before you pull or push.**
7. **Stash when switching context.**

## Context

- If there's no remote, ask whether this is a different repository and what its path is. Don't ask for a URL first.
- Check `git config --get remote.origin.url` before saying there's no remote.
- If a file you're shown is from a different workspace, ask whether to switch to that directory first.

## Tools

### Branch Management
- `GetCurrentBranch()` — Get the current branch name
- `CreateBranch(branchName)` — Create a new branch from HEAD
- `SwitchBranch(branchName)` — Switch to an existing branch
- `DeleteBranch(branchName, force)` — Delete a branch (safe or force)
- `ListBranches(all)` — List branches (local or local + remote)

### Staging & Committing
- `Stage(pathSpec)` — Stage files for commit (use `.` for all)
- `Unstage(pathSpec)` — Unstage files (use `.` for all staged changes)
- `Commit(message)` — Commit staged changes
- `AmendCommit(noEdit)` — Amend the previous commit
- `DiffUnstaged(filePath)` — Show unstaged changes
- `DiffStaged(filePath)` — Show staged changes

### Cleanup & Reset
- `DiscardUnstaged(pathSpec)` — Discard unstaged changes in working directory
- `ResetHard(target)` — Reset working directory to a commit (default: HEAD)
- `Stash(message)` — Stash current changes
- `ListStashes()` — List all stashed changes
- `ApplyStash(stashRef, pop)` — Apply or pop a stash

### History & Inspection
- `GetStatus()` — Get current git status (short format)
- `GetLog(count)` — Show recent commits (default: 10)
- `ShowCommit(commitRef)` — Show details of a specific commit
- `HasUncommittedChanges()` — Check for uncommitted changes
- `GetRepositoryInfo()` — Show branch, remotes, and status summary

### Remote Operations
- `Push(remote, branch, force)` — Push commits to remote (uses `--force-with-lease`)
- `Pull(remote, branch)` — Pull latest changes from remote
- `Fetch(remote)` — Fetch updates from remote without merging
- `GetRemoteUrl(remoteName)` — Get the URL of a remote
- `ListRemotes()` — List all remotes with URLs
- `AddRemote(name, url)` — Add a new remote

### Merging, Rebasing & Cherry-picking
- `Merge(source, message)` — Merge a branch into current branch
- `Rebase(onto, abort)` — Rebase current branch onto another (or abort rebase)
- `CherryPick(commitRef)` — Cherry-pick a commit onto current branch

### Tags
- `CreateTag(tagName, message)` — Create a tag (annotated if message provided)
- `ListTags()` — List all tags
- `DeleteTag(tagName)` — Delete a tag

### Configuration
- `GetConfig(key)` — Get a git config value (or all if key is empty)
- `SetConfig(key, value)` — Set a git config value

## Workflows

### Feature branch
```
GetCurrentBranch()
CreateBranch("feature/my-feature")
SwitchBranch("feature/my-feature")
# ... make changes with WriteWorkspaceFile, etc.
Stage(".")
Commit("feat: add my feature")
Push("origin", "feature/my-feature")
```

### Review before pushing
```
GetStatus()
DiffStaged()
DiffUnstaged()
GetLog(5)
```

### Stash and switch
```
Stash("WIP: feature X")
SwitchBranch("main")
Pull("origin", "main")
# ... work on something else ...
SwitchBranch("feature/my-feature")
ApplyStash("stash@{0}", pop=true)
```

### Investigate history
```
GetLog(20)
ShowCommit("HEAD~3")
GetRepositoryInfo()
```
