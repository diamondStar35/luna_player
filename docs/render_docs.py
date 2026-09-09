#!/usr/bin/env python3
"""Render the language-specific Markdown guides as standalone HTML pages."""

from __future__ import annotations

import html
from pathlib import Path
import sys

try:
    import markdown
except ImportError:
    print(
        "Python-Markdown is required. Install it with "
        "'python -m pip install -r docs/requirements.txt'.",
        file=sys.stderr,
    )
    raise SystemExit(1)


STYLE = """
    :root {
      color-scheme: light dark;
      --background: #0e1117;
      --surface: #161b22;
      --text: #e6edf3;
      --link: #58a6ff;
      --border: #30363d;
      --code: #11161d;
    }
    @media (prefers-color-scheme: light) {
      :root {
        --background: #f6f8fa;
        --surface: #ffffff;
        --text: #1f2328;
        --link: #0969da;
        --border: #d0d7de;
        --code: #f6f8fa;
      }
    }
    body {
      margin: 0;
      background: var(--background);
      color: var(--text);
      font: 16px/1.55 "Segoe UI", Tahoma, sans-serif;
    }
    main {
      box-sizing: border-box;
      max-width: 960px;
      margin: 24px auto;
      padding: 24px;
      background: var(--surface);
      border: 1px solid var(--border);
      border-radius: 12px;
      overflow-wrap: anywhere;
    }
    h1, h2, h3, h4 { line-height: 1.25; }
    h1 { margin-top: 0; }
    a { color: var(--link); }
    code, pre { font-family: Consolas, "Cascadia Mono", monospace; background: var(--code); }
    code { border: 1px solid var(--border); border-radius: 5px; padding: 0.1em 0.3em; }
    pre { border: 1px solid var(--border); border-radius: 8px; padding: 12px; overflow: auto; }
    pre code { border: 0; padding: 0; }
    table { border-collapse: collapse; width: 100%; }
    th, td { border: 1px solid var(--border); padding: 8px 10px; text-align: left; }
"""


def render(source: Path) -> None:
    text = source.read_text(encoding="utf-8-sig")
    title = next(
        (line[2:].strip() for line in text.splitlines() if line.startswith("# ")),
        source.stem.replace("-", " ").title(),
    )
    body = markdown.markdown(
        text,
        extensions=["extra", "fenced_code", "sane_lists", "tables", "toc"],
        extension_configs={"toc": {"toc_depth": "2-2"}},
        output_format="html5",
    )
    language = source.parent.name
    output = source.with_suffix(".html")
    page = f"""<!doctype html>
<html lang="{html.escape(language, quote=True)}">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>{html.escape(title)}</title>
  <style>{STYLE}</style>
</head>
<body>
  <main>
{body}
  </main>
</body>
</html>
"""
    if not output.exists() or output.read_text(encoding="utf-8") != page:
        output.write_text(page, encoding="utf-8")
    print(f"Rendered HTML: {output}")


def main() -> int:
    docs = Path(__file__).resolve().parent
    guides = sorted(docs.glob("*/user-guide.md"))
    if not guides:
        print(f"No language-specific user guides found under {docs}.", file=sys.stderr)
        return 1
    for guide in guides:
        render(guide)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
