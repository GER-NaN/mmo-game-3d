# Codex

A page for every term in the game: places, people, items, the things standing in the
world, the terminal and its apps, skills and careers, money, groups and travel. Each
page says what the thing is and whether it is built, decided or still an idea.

Any doc that mentions a term links to its page, on the first mention, and each page
lists the docs that mention it. Nobody writes those links: the wiki makes them when it
is published (`tools/wiki/hooks/codex.py`).

A page's header lists its other names, so "GPU", "GPUs" and "GPU core" all find the
same page:

```
---
aliases:
  - GPU
  - GPUs
match_case: true   # optional: only match the names as written
---
```

New terms are found by the Codex Sweeper (`tools/codex-sweeper`), which adds an empty
page for each under **New**, marked TODO, with the sentences it was seen in.
