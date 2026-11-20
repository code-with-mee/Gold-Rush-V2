#!/usr/bin/env python3
"""blocks2.py get <phase> <Dir/File.cs> <out.cs> | set <phase> <Dir/File.cs> <in.cs>"""
import html, os, re, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import phasecheck2 as pc

def find(n, rel):
    path = f"{pc.DOCS}/phase-{n:02d}.html"
    t = open(path, encoding="utf-8").read()
    hits = [pos for pos, kind, f, _ in pc.events(n) if kind == "typed" and f == rel]
    if len(hits) != 1:
        sys.exit(f"phase {n} {rel}: {len(hits)} blocks")
    return path, t, pc.BLOCK_RE.match(t, hits[0])

esc = lambda s: s.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")
if __name__ == "__main__":
    cmd, n, rel, f = sys.argv[1], int(sys.argv[2]), sys.argv[3], sys.argv[4]
    path, t, m = find(n, rel)
    if cmd == "get":
        open(f, "w", encoding="utf-8").write(html.unescape(m.group(1)))
    else:
        code = open(f, encoding="utf-8").read().rstrip("\n")
        open(path, "w", encoding="utf-8").write(t[:m.start(1)] + esc(code) + t[m.end(1):])
        print("updated", path, rel)
