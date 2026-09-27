# codex-sweeper

Finds game terms that have no Codex page yet and adds an empty page for each, marked
TODO, under `docs/codex/new/`, with the sentences it was seen in. It writes no
definitions and never changes a page that exists.

```
python tools/codex-sweeper/sweep.py            add pages for new terms
python tools/codex-sweeper/sweep.py --dry-run  only list them
```

Then publish the wiki (`scripts/wiki-publish.ps1`); the new pages show under
**Codex > New**.

**Where it looks:**

- The code's own lists of named things: items (`ItemCatalog.cs`), terminal apps,
  careers, skills, and named people in the zone scenes.
- The design docs (`world.md`, `backlog.md`, `features/`, `planning/`, `fiction/`):
  bold phrases inside a sentence, and capitalised phrases of two to four words used
  more than once. Engineering, sources and outdated docs are left out: they would bury
  the game's terms under old code and model proposals.

**A term is new** when no Codex page has it as its title or as an alias, in any case,
plural or not.

**What to do with a TODO page:**

- A game term: write it, and move it to its section in `docs/codex/`.
- The same thing as a page that exists: delete it, and add the name to that page's
  aliases.
- Not a game term: delete it, and add the name to `docs/codex/.sweeper-ignore`, so
  it is not found again.

Needs Python 3 and nothing else.
