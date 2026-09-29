---
name: mvp-design
description: Run an MVP design session for one topic of this game. A short Q&A that works out the smallest version of the topic that is built right the first time - its main mechanics, how a player uses it and why, where it lives (server, client), what it keeps (persistence), the art it needs, and the framework the fuller version will grow from - recorded in one file in the author's words. Not the topic's full scope, and never its release scope. Use when the user says "MVP for X", "mini Q&A on X", "/mvp-design X".
---

# MVP design

An MVP design session is a short Q&A interview on one topic. It works out the minimum
viable version of the topic, and the frame it is built on, so the fuller version later
grows from it instead of replacing it. The output is one file, `docs/features/<topic>-mvp.md`.
The file is the record. Chat is the interview. It stays after a full `feature-design`
session on the topic, which reads it and builds on it (`docs/features/<topic>.md`).

**What it is not.** Not the full design of the topic (that is `feature-design`). Not a
release plan: never ask what goes in the first playable, the beta or the full game.
Never scope a question to "the first build".

**How deep.** Deep enough that the MVP is not rewritten later: the main mechanics, how
a player uses it, what the server owns, what is kept, what it looks like. Not every edge
case. Where a behaviour follows sensibly from what is agreed, assume it, write the
assumption down, and let the author overturn it in one pass at the end, instead of
asking.

## Rules that apply to the whole session

- The author's words are the design. Record answers verbatim or near it, as
  blockquotes, headed `Answer (date):`. Never paraphrase an answer into something the
  author did not say.
- Label everything the model adds, set apart inside the blockquote:
  - `Consequence noted:` the model's reading of what an answer implies.
  - `Assumed:` a behaviour the model fills in rather than asks about. Every one is also
    listed under "Assumptions to confirm".
  - `Options offered:` the model's suggestions, only when the author asked for ideas.
    Say which one was chosen, or "none chosen".
  - `Note:` corrects the record.
  - `Decision (date):` the author's pick after options were offered, verbatim.
- One question at a time, numbered `M1, M2, ...`; a follow-up raised by an answer is
  `M3a, M3b`, asked before the next numbered question.
- A question is the question only. No strawman, no example answer, no options. Offer
  options only when the author asks, or answers "I don't know": then state the facts
  from the code, give the options, say which you would take and why, and end with
  "Decision: pending the author's word".
- Prefer the load-bearing question, the one other answers depend on. Skip a question
  whose answer can be assumed safely; assume it instead.
- Aim for about ten to fifteen questions in all. More means the session is turning
  into a full design: say so, and ask whether to stop at the MVP.
- Chat stays short: "Recorded." plus the next question. Reflections go in the file.
  When the author asks "what do you think" or "summarise", answer in full.
- Write the file as you go, after every answer.
- Chat and file follow the project's Simplified Technical English rules
  (`CLAUDE.local.md`). The file has LF line endings (`.gitattributes`).

## The areas to cover

Every session covers these, in about this order, each only as deep as the MVP needs:

1. **Why.** Why a person at the keyboard would do this at all: what it gives them, what
   it means in the world. A little fiction, only as much as the motive needs; read
   `docs/world.md` for what is already there before asking.
2. **What.** The main mechanics: what the player does, step by step, and what the game
   does back. What it costs, what it gives, how it ends.
3. **Where.** What the server owns and decides, what the client shows and sends. Which
   existing systems it touches (by their current names).
4. **Keeps.** What survives a logout, a server restart, a zone change: what is
   persisted, per player or per world, and what is only in memory.
5. **Looks.** The art the MVP needs: models, icons, panels, sounds. Check the packs we
   have (KayKit, the icon sets) before asking for new art; a placeholder is fine when
   it says so.
6. **Frame.** What the fuller version will add, named but not designed, and the seams
   the MVP leaves for it (a type, a table, a message, a panel that has room), so adding
   it later is new code, not a rewrite.

## Steps

1. **Topic.** Ask for the author's basic idea of the topic if the request has none.
   Read `docs/world.md` for every mention first; cite the sections in the file header.
2. **Create the file** at `docs/features/<topic>-mvp.md` from the template below, with
   the author's basic idea under "Developer thoughts", verbatim.
3. **Already decided.** Before M1, write "Already decided" in the file: every decision
   in `docs/world.md` touching the topic, with its tag; what the code builds today
   (current names), live or scaffolding; what is deferred in `TODO.md` or
   `docs/backlog.md`. Say the short version in chat. Questions come from the gaps.
4. **Q&A** through the six areas. Read the code before the Where and Keeps questions,
   and write "Facts held in mind" in the file first: what the code does today for the
   pieces the topic touches, in play terms. Flag every clash with "Already decided", or
   with an architecture rule in `CLAUDE.local.md`, in the next question, and record the
   author's choice.
5. **Assumptions to confirm.** Before the Outcome, list every `Assumed:` item in one
   place and ask the author to confirm or change them in one answer.
6. **Outcome.** The MVP as a build list, each item traceable to an answer (`[M4]`):
   - Mechanics: what is built.
   - Server and client: what each owns, the messages.
   - Persistence: tables or fields, and what is only in memory.
   - Art: what is needed, what is a placeholder.
   - Tests: the rules under xUnit.
   - Frame: the seams left for the fuller version.
   Then "Beyond the MVP": what the fuller version adds, named only.
7. **Close.** "Consequences": which existing decisions or docs this changes. Propose,
   and do only when the author says yes: fold decisions into `docs/world.md` in the
   author's words, tagged `[C-<date>]`; move undecided ideas to `docs/backlog.md`.
   Save a project memory naming the file and its status. Code starts after the
   session, and only when the author says go.

## File template

```markdown
# <Topic>: MVP

**Date:** <YYYY-MM-DD>
**Status:** In session | Agreed | Built
**Scope:** MVP (the fuller version is under "Beyond the MVP", not designed here)
**Sources read:** docs/world.md sections <n, n>, <other files>

<One or two sentences: the topic, and what the MVP is for.>

The answers are the author's own words, recorded verbatim or near it, one question at a
time. Model additions are set apart and labelled: "Consequence noted", "Assumed",
"Options offered", "Note".

## Already decided

- <decision> [tag]

Built:

- <what the code does today, by current names>

Deferred:

- <what is deliberately not built, and where that is recorded>

## Developer thoughts

> <the author's basic idea, verbatim>

## Facts held in mind

- <what the code does today for the pieces this touches>

## Q&A

**M1.** <question>

> Answer (<date>): <verbatim>
>
> Assumed: <a behaviour filled in, optional>

## Assumptions to confirm

- <assumption> [M3]: confirmed | changed to ...

## Outcome

Mechanics:

- <decision> [M2]

Server and client:

- <decision> [M5]

Persistence:

- <decision> [M6]

Art:

- <need> [M7]

Tests:

- <rule test>

Frame:

- <seam left for the fuller version> [M9]

## Beyond the MVP

- <what the fuller version adds, named only>

## Consequences

- <what changes in world.md, backlog.md, engineering/, or existing code decisions>
```
