---
name: feature-design
description: Run a feature design session for this game. One session covers two forks, the feature (product, meaning, behaviour, form, art) and the technology (how client and server make it work), each as a Q&A loop, recorded in one file in the author's words. Use when the user says "let's add X to the game", "design X", "/feature-design X", or wants to work out a new feature or framework before code.
---

# Feature design

A feature design session is a Q&A interview in two forks. The output is one file per
feature under `docs/features/`. The file is the record. Chat is the interview.

## Rules that apply to the whole session

- The author's words are the design. Record answers verbatim or near it, as
  blockquotes, headed `Answer (date):`. Never paraphrase an answer into something the
  author did not say.
- Label everything the model adds, set apart inside the blockquote:
  - `Consequence noted:` the model's reading of what an answer implies.
  - `Options offered:` the model's suggestions, only when the author asked for ideas.
    Say which one was chosen, or "none chosen".
  - `Note:` corrects the record.
  - `Decision (date):` the author's pick after options were offered, verbatim.
  - A reading the author overturns stays in the file, marked "(withdrawn, see the
    Note below)", so the correction is readable.
- The author asks questions too. Record one as a follow-up with "(author's
  question)" after the number, and answer it in the file under "Options offered
  (asked for)".
- "I don't know", "not sure", "would need to think" from the author is the trigger
  to offer a reading: state the facts from the code, give the options, say which one
  you would take and why, and end with "Decision: pending the author's word". Then
  ask for the decision in chat.
- One question at a time. Number questions `F1, F2, ...` in the feature fork and
  `T1, T2, ...` in the technology fork.
- A question is the question only. No strawman, no example answer, no options. The
  goal is to extract what the author did not say in the basic idea, usually a
  significant gap. Offer options only when the author asks for ideas.
- When an answer brings up something surprising that needs more scrutiny, add
  `Follow-up:` under that answer in the file and ask it before the next numbered
  question. Number follow-ups `F3a, F3b, ...`.
- Chat stays short: "Recorded." plus the next question. Reflections go in the file, not
  in chat. When the author asks "what do you think" or "summarise", answer in full.
- Write the file as you go. Update it after every answer, not at the end.
- Chat replies follow the project's Simplified Technical English rules
  (`CLAUDE.local.md`). The file follows the same style.
- CRLF line endings on the file after every edit.
- A question may stay open on purpose. Say so in the file and say why.

## Steps

1. **Basic idea.** Ask the author for the basic idea of the feature if the request did
   not include one. Read `docs/world.md` for every mention of the feature before asking
   anything. Cite the section numbers you read in the file header.
2. **Create the file** at `docs/features/<slug>.md` from the template below. Put the
   author's basic idea under "Feature design / Developer thoughts", verbatim.
3. **Feature fork.** Ask clarifying questions until the feature is agreed: what it is
   in the fiction, what the player does with it, what it costs, what it looks like
   (form and art), what it does not do yet. Prefer the load-bearing question, the one
   other answers depend on. When agreed,
   write "Outcome" for the fork: the decisions as a list, each traceable to an answer
   (`[F3]`), plus "Deferred" for what is left open.
4. **Technology fork.** Read the current code before asking: the simulation, the
   state and view types, the messages, the client caches and screens the feature
   touches. Ask the author for their technical thoughts first, verbatim under
   "Technology design / Developer thoughts". Before T1, write "Facts held in mind"
   in the file and say them in chat: what the code does today for the pieces the
   feature touches, in play terms, with current names. Say which parts are live in
   play and which are scaffolding nothing reaches yet. The author may not hold the
   built state in mind, and a question that assumes it stalls. Then clarify until there is one agreed
   design: where state lives, what the server owns, what messages exist, what the
   client renders, what is persisted, what tests prove it. Name current types by
   their current names. Flag every place the design touches an architecture
   convention in `CLAUDE.local.md`. When agreed, write "Outcome" with the same
   traceability (`[T2]`), plus "Deferred".
5. **Close.** Add "Consequences" at the end: which existing decisions this changes,
   which docs need an update. Then propose, and do only when the author says yes:
   - Fold feature decisions into `docs/world.md` in the author's words, tagged
     `[C-<date>]`, and remove them from `docs/backlog.md` if they were there.
   - Move undecided ideas to `docs/backlog.md`.
   - Write or update the engineering doc in `docs/engineering/` once the code exists,
     not before.
   Code starts after the session, in its own branch and PR, and only when the author
   says go.
   Also at close: save a project memory that names the session file, its status,
   and whether the build started, so a fresh session finds it. When the technology
   Outcome is large, offer a split into PRs in the Consequences, by dependency, not
   by schedule.

## World and game gatekeeping

A feature must not clash with what is built, what is decided in `docs/world.md`, or
what is planned in `docs/planning/first-playable.md` and `docs/backlog.md`, unless the author
changes the earlier decision on purpose. The gate runs three times:

1. **Gate in, before F1.** Write "Already decided" in the file: every decision in
   `docs/world.md` that touches the feature, with its tag; what the code already
   builds (current type and message names); what is deferred in `CLAUDE.local.md` or
   `docs/backlog.md`. This is the list the feature has to fit. Questions come from it.
2. **Gate during Q&A.** When an answer clashes with an item on that list, or with
   another answer in the session, flag it in the next question, not later. Write it in
   the file as `Note: clashes with [Q12] ...` under the answer, and ask which wins. The
   author's choice is recorded, and the losing decision is listed in Consequences.
3. **Gate out, before Outcome is final.** Walk the "Already decided" list once more
   against the Outcome. Write the result in "Gatekeeping" at the end of the file:
   each item either fits, or is changed on purpose (say by which answer), or is left
   open. Do not write Outcome as agreed while a clash is unresolved.

## File template

```markdown
# <Title>

**Date:** <YYYY-MM-DD>
**Status:** In session | Agreed | Built
**Sources read:** docs/world.md sections <n, n>, <other files>
**Build from:** the two Outcome sections and the T-list of tests. The Q&A is the
record of how they were reached.

<One or two sentences: what the feature is and why it is being designed now, for
example "first equippable item, so the equipment framework comes first".>

The answers are the author's own words, typed in conversation and recorded verbatim or
near it, one question at a time. Model additions are set apart and labelled:
"Consequence noted", "Options offered", "Note".

## Already decided

Decisions in force that this feature must fit, or change on purpose.

- <decision> [tag]

Built:

- <what the code does today, by current names>

Deferred:

- <what is deliberately not built, and where that is recorded>

## Feature design

### Developer thoughts

> <verbatim>

### Q&A

**F1.** <question>

> Answer (<date>): <verbatim>
>
> Consequence noted: <model reading, optional>

**F1a.** Follow-up: <question raised by the answer>

> Answer (<date>): <verbatim>

### Outcome

- <decision> [F1]

Deferred:

- <open item and why>

## Technology design

### Developer thoughts

> <verbatim>

### Q&A

**T1.** <question>

> Answer (<date>): <verbatim>

### Outcome

- <decision> [T1]

Deferred:

- <open item and why>

## Gatekeeping

Each "Already decided" item against the Outcome.

- <decision> [tag]: fits | changed by [F3] | open

## Consequences

- <what changes in world.md, backlog.md, engineering/, or existing code decisions>
```
