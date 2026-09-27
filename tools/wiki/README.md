# wiki

The docs as a browsable site: Material for MkDocs builds `docs/` into `site/` (ignored
by git), and the `mmo3d-wiki` container (`docker/docker-compose.yml`) serves it at
http://localhost:8000.

The Markdown in `docs/` stays the source. Edit a doc, then publish:

```
.\scripts\wiki-publish.ps1
```

`scripts/client-up.ps1` also publishes, with `-IfChanged`: only when a file in `docs/`
or here is newer than the built site, so a launch with no doc change costs nothing.

- `mkdocs.yml`: the site, its theme, and the link checks. A broken link is printed on
  publish and does not stop it.
- `Dockerfile`: the builder image, Material pinned, plus the `awesome-nav` plugin.
- `docs/.nav.yml`: the top-level order of the sidebar. Everything below it follows the
  folders; a page's title is its first heading. A new doc appears by itself.

Material for MkDocs is in maintenance mode (fixes until May 2027). Its successor,
Zensical, reads the same `mkdocs.yml`. MkDocs 2.0 drops plugins, so the image stays on
Material 9, which pins MkDocs 1.
