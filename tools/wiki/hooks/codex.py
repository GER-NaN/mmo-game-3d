"""Codex links, made at publish time.

Every page under docs/codex/ is a term. Its names are its title (the first "# " line)
and the aliases in its front matter. Any doc that mentions a name links to the term's
page on the first mention; each term page gets the docs that mention it. The Markdown
files never change: the links exist only in the built site.

Skipped: code, existing links, headings, HTML, and docs/outdated/, whose terms may
have old meanings.
"""

import posixpath
import re

import yaml

CODEX = "codex/"
SKIPPED = ("outdated/",)

# Spans a link must not land in: fenced code, inline code, links and images, reference
# definitions, HTML tags, bare URLs, headings.
MASK = re.compile(
    r"```.*?```|~~~.*?~~~"
    r"|`[^`\n]*`"
    r"|!?\[[^\]\n]*\]\([^)\n]*\)|!?\[[^\]\n]*\]\[[^\]\n]*\]"
    r"|^[ \t]*\[[^\]\n]+\]:[^\n]*$"
    r"|<[^>\n]+>"
    r"|https?://\S+"
    r"|^#{1,6} [^\n]*$",
    re.S | re.M,
)

_terms = {}      # term page uri -> {"title", "names", "match_case"}
_lookup = {}     # lower-case name -> term page uri
_pattern = None
_titles = {}     # every page uri -> its title
_mentions = {}   # term page uri -> set of page uris that mention it


def _split_front_matter(text):
    if text.startswith("---\n"):
        end = text.find("\n---", 4)
        if end != -1:
            meta = yaml.safe_load(text[4:end]) or {}
            return meta, text[end + 4:]
    return {}, text


def _title_of(body, fallback):
    for line in body.splitlines():
        if line.startswith("# "):
            return line[2:].strip()
    return fallback


def _is_term(uri):
    return uri.startswith(CODEX) and posixpath.basename(uri) != "README.md"


def _is_skipped(uri):
    return uri.startswith(SKIPPED)


def _name_pattern(name):
    words = [re.escape(w) for w in name.split()]
    body = r"\s+".join(words)
    # Plurals come free: "terminal" also finds "terminals".
    if name[-1:].isalpha() and not name.endswith("s"):
        body += r"(?:s|es)?"
    return body


def _find(text):
    """Yields (start, end, term uri) for every name in text outside the mask."""
    masked = [(m.start(), m.end()) for m in MASK.finditer(text)]
    spot = 0
    for m in _pattern.finditer(text):
        while spot < len(masked) and masked[spot][1] <= m.start():
            spot += 1
        if spot < len(masked) and masked[spot][0] < m.end():
            continue
        uri = _resolve(m.group(0))
        if uri is not None:
            yield m.start(), m.end(), uri


def _resolve(found):
    plain = " ".join(found.split())
    for cut in (0, 1, 2):
        stem = plain[: len(plain) - cut] if cut else plain
        uri = _lookup.get(stem.lower())
        if uri is None:
            continue
        term = _terms[uri]
        if term["match_case"] and stem not in term["names"]:
            return None
        return uri
    return None


def on_files(files, config):
    global _pattern
    _terms.clear()
    _lookup.clear()
    _titles.clear()
    _mentions.clear()

    pages = list(files.documentation_pages())
    for f in pages:
        meta, body = _split_front_matter(f.content_string)
        _titles[f.src_uri] = _title_of(body, posixpath.splitext(posixpath.basename(f.src_uri))[0])
        if _is_term(f.src_uri):
            names = [_titles[f.src_uri]] + [str(a) for a in (meta.get("aliases") or [])]
            _terms[f.src_uri] = {"title": _titles[f.src_uri], "names": names, "match_case": bool(meta.get("match_case"))}
            for name in names:
                _lookup[name.lower()] = f.src_uri

    if not _lookup:
        _pattern = None
        return files

    # Longest first, so "Terminal World" wins over "Terminal".
    names = sorted(_lookup.keys(), key=len, reverse=True)
    _pattern = re.compile(r"(?<!\w)(?:" + "|".join(_name_pattern(n) for n in names) + r")(?!\w)", re.I)

    for f in pages:
        if _is_skipped(f.src_uri):
            continue
        _, body = _split_front_matter(f.content_string)
        for _, _, uri in _find(body):
            if uri != f.src_uri:
                _mentions.setdefault(uri, set()).add(f.src_uri)
    return files


def _link(from_uri, to_uri):
    return posixpath.relpath(to_uri, posixpath.dirname(from_uri) or ".")


def _list(from_uri, uris):
    ordered = sorted(uris, key=lambda u: _titles.get(u, u).lower())
    return "\n".join("- [" + _titles.get(u, u) + "](" + _link(from_uri, u) + ")" for u in ordered)


def on_page_markdown(markdown, page, config, files):
    uri = page.file.src_uri
    if _pattern is None or _is_skipped(uri):
        return markdown

    linked = set()
    cuts = []
    for start, end, target in _find(markdown):
        if target == uri or target in linked:
            continue
        linked.add(target)
        cuts.append((start, end, target))

    for start, end, target in reversed(cuts):
        markdown = markdown[:start] + "[" + markdown[start:end] + "](" + _link(uri, target) + ")" + markdown[end:]

    if uri in _terms:
        mentions = _mentions.get(uri, set())
        docs = [u for u in mentions if not _is_term(u)]
        related = [u for u in mentions if _is_term(u)]
        if docs:
            markdown += "\n\n## Mentioned in\n\n" + _list(uri, docs) + "\n"
        if related:
            markdown += "\n\n## Related terms\n\n" + _list(uri, related) + "\n"
    elif uri.startswith(CODEX):
        markdown += "\n\n## All terms\n\n" + _list(uri, _terms.keys()) + "\n"

    return markdown
