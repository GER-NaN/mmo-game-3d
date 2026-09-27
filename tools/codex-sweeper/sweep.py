"""Codex Sweeper: finds game terms that have no Codex page yet, and adds an empty page
for each under docs/codex/new/, marked TODO, with the sentences it was seen in.

It never changes a page that exists, and it writes no definitions. A term that is not
a game term goes in docs/codex/.sweeper-ignore (one name a line) and is not found again.

    python tools/codex-sweeper/sweep.py            add pages for new terms
    python tools/codex-sweeper/sweep.py --dry-run  only list them
"""

import argparse
import datetime
import os
import re
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
DOCS = os.path.join(ROOT, "docs")
CODEX = os.path.join(DOCS, "codex")
NEW = os.path.join(CODEX, "new")
IGNORE = os.path.join(CODEX, ".sweeper-ignore")

# The design docs. Engineering, sources and outdated talk about the old code and the
# models' proposals, and would bury the game's own terms.
DESIGN_DOCS = ["world.md", "backlog.md", "features", "planning", "fiction"]

# Where the code names the game's things, and the pattern that finds each name. A
# second group, when there is one, is the thing's own description.
CODE_SOURCES = [
    ("src/Rules/Items/ItemCatalog.cs", r'new ItemDefinition\(ItemType\.\w+, "([^"]+)", "([^"]+)"'),
    ("src/Rules/Terminals/TerminalApps.cs", r'new TerminalApp\([^,]+, "([^"]+)", "([^"]*)"'),
    ("src/Rules/Skills/Careers.cs", r'^\s*"([A-Z][a-z]+(?: [A-Z][a-z]+){0,2})",\s*\n\s*"([^"]+)"'),
    ("src/Rules/Skills/Skills.cs", r'return "([A-Z][a-z]+(?: [a-z]+)?)";'),
    ("game", r'PersonName = "([^",]+)(?:, ([^"]+))?"'),
]

# Capitalised words that start phrases but are not terms.
NOT_TERMS = {
    "The", "A", "An", "This", "That", "It", "In", "On", "At", "For", "If", "When", "And",
    "But", "Or", "Not", "No", "Yes", "One", "Two", "Each", "Every", "Some", "All", "What",
    "Who", "How", "Why", "Where", "Which", "Then", "So", "As", "By", "With", "From", "To",
    "Of", "Is", "Are", "Was", "Be", "Do", "Can", "May", "Should", "Must", "Will", "I",
    "We", "You", "He", "She", "They", "My", "Our", "Your", "Its", "Their", "Today", "Now",
    "Later", "Here", "There", "Also", "Only", "Both", "Once", "See", "Note", "Answer",
    "Consequence", "Question", "Q", "F", "T", "DT", "M", "B", "Done", "Planned",
    "Partial", "Undecided", "Decided", "Deferred", "Built", "Open", "Agreed",
}


def read(path):
    with open(path, encoding="utf-8") as f:
        return f.read()


def rel(path):
    return os.path.relpath(path, ROOT).replace(os.sep, "/")


def known_names():
    """Every title and alias already in the Codex, lower-case."""
    names = set()
    for folder, _, files in os.walk(CODEX):
        for name in files:
            if not name.endswith(".md") or name == "README.md":
                continue
            text = read(os.path.join(folder, name))
            for line in text.splitlines():
                if line.startswith("# "):
                    names.add(line[2:].strip().lower())
                    break
            if text.startswith("---\n"):
                head = text[4:text.find("\n---", 4)]
                for line in head.splitlines():
                    m = re.match(r"\s+-\s+(.+)$", line)
                    if m:
                        names.add(m.group(1).strip().strip("\"'").lower())
    return names


def ignored():
    if not os.path.exists(IGNORE):
        return set()
    return {line.strip().lower() for line in read(IGNORE).splitlines() if line.strip() and not line.startswith("#")}


def singular(name):
    for end in ("es", "s"):
        if name.endswith(end) and len(name) > len(end) + 2:
            yield name[: -len(end)]
    yield name


def is_known(name, names):
    return any(form in names for form in singular(name.lower()))


def design_files():
    for entry in DESIGN_DOCS:
        path = os.path.join(DOCS, entry)
        if os.path.isfile(path):
            yield path
        for folder, _, files in os.walk(path):
            for name in sorted(files):
                if name.endswith(".md"):
                    yield os.path.join(folder, name)


def plain(text):
    text = re.sub(r"^[ \t]*>[ \t]?", "", text, flags=re.M)
    text = re.sub(r"```.*?```", " ", text, flags=re.S)
    text = re.sub(r"`[^`\n]*`", " ", text)
    text = re.sub(r"\[([^\]\n]*)\]\([^)\n]*\)", r"\1", text)
    return text


def sentence_around(text, start, end):
    left = max(text.rfind(". ", 0, start), text.rfind("\n\n", 0, start), text.rfind("\n- ", 0, start))
    right_dot = text.find(". ", end)
    right_par = text.find("\n\n", end)
    right = min(r for r in (right_dot + 1 if right_dot != -1 else len(text), right_par if right_par != -1 else len(text)))
    sentence = " ".join(text[left + 1:right].replace("*", "").split()).lstrip("- ")
    return sentence[:300]


def from_code():
    """(name, where, context) for every game name the code declares."""
    for source, pattern in CODE_SOURCES:
        path = os.path.join(ROOT, source)
        files = [path] if os.path.isfile(path) else [
            os.path.join(folder, name)
            for folder, _, names in os.walk(path)
            for name in names if name.endswith(".tscn")
        ]
        for file in files:
            text = read(file)
            for m in re.finditer(pattern, text, re.M):
                context = m.group(2) if m.lastindex and m.lastindex >= 2 and m.group(2) else ""
                yield m.group(1).strip(), rel(file), context


def from_docs():
    """(name, where, sentence) for terms the design docs set apart: bold phrases, and
    capitalised phrases of two to four words used more than once."""
    phrase = re.compile(r"(?<![.!?:]\s)(?<!^)\b([A-Z][a-z]+(?: (?:of|the) [A-Z][a-z]+| [A-Z][a-z]+){1,3})\b", re.M)
    # Bold at the start of a line or a list item is a label ("**Done when** ..."), not a term.
    bold = re.compile(r"(?<!^)(?<!^- )(?<!^  - )\*\*([A-Z][A-Za-z' -]{2,40}?)\*\*", re.M)
    seen = {}
    for path in design_files():
        text = plain(read(path))
        for m in bold.finditer(text):
            name = m.group(1).strip()
            if 1 <= len(name.split()) <= 4 and not name.endswith(":"):
                seen.setdefault(name, []).append((rel(path), sentence_around(text, m.start(), m.end()), True))
        for m in phrase.finditer(text):
            name = m.group(1)
            if name.split()[0] in NOT_TERMS:
                continue
            seen.setdefault(name, []).append((rel(path), sentence_around(text, m.start(), m.end()), False))
    for name, uses in seen.items():
        if any(is_bold for _, _, is_bold in uses) or len(uses) >= 2:
            for where, sentence, _ in uses:
                yield name, where, sentence


def slug(name):
    return re.sub(r"[^a-z0-9]+", "-", name.lower()).strip("-")


def main():
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    parser.add_argument("--dry-run", action="store_true", help="list new terms, write nothing")
    args = parser.parse_args()

    names = known_names()
    skip = ignored()
    found = {}
    for source in (from_code(), from_docs()):
        for name, where, context in source:
            key = name.lower()
            if is_known(name, names) or key in skip or name.split()[0] in NOT_TERMS:
                continue
            entry = found.setdefault(key, {"name": name, "seen": []})
            if len(entry["seen"]) < 3 and (where, context) not in entry["seen"]:
                entry["seen"].append((where, context))

    existing = {os.path.splitext(n)[0] for _, _, files in os.walk(CODEX) for n in files}
    added = 0
    today = datetime.date.today().isoformat()
    for key in sorted(found):
        entry = found[key]
        page = slug(entry["name"])
        if not page or page in existing:
            continue
        lines = ["- `" + where + "`" + (': "' + context + '"' if context else "") for where, context in entry["seen"]]
        print(entry["name"] + "  (" + ", ".join(w for w, _ in entry["seen"]) + ")")
        if args.dry_run:
            continue
        os.makedirs(NEW, exist_ok=True)
        text = (
            "# " + entry["name"] + "\n\n"
            "**Status:** TODO. Found by the Codex Sweeper on " + today + "; not written yet. "
            "Write it and move it to its section, or delete it and add the name to "
            "`docs/codex/.sweeper-ignore`.\n\n"
            "## Seen in\n\n" + "\n".join(lines) + "\n"
        )
        with open(os.path.join(NEW, page + ".md"), "w", encoding="utf-8", newline="\n") as f:
            f.write(text)
        existing.add(page)
        added += 1

    if args.dry_run:
        print(str(len(found)) + " new terms (dry run, nothing written)")
    else:
        print(str(added) + " TODO pages added to docs/codex/new/")
    return 0


if __name__ == "__main__":
    sys.exit(main())
