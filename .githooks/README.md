# Git hooks

Git does not commit the hooks in `.git/hooks`, so the hooks this repository uses live
here and are turned on per clone:

```
git config core.hooksPath .githooks
```

## pre-commit

Runs `dotnet format` over the C# files in the commit and stages the result.

The editor already formats on save (`.vscode/settings.json`). This hook covers the files
that never pass through the editor: the ones an agent or a script writes.

Notes:

- One run costs about six seconds, because `dotnet format` loads the solution. The
  number of files hardly matters.
- A file that holds both staged and unstaged changes is reported and left alone, so the
  hook never commits a change that was left out on purpose.
- When `dotnet format` fails, for example with no SDK on the machine, the commit goes
  ahead and the hook says so.
