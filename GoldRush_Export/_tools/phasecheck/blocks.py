#!/usr/bin/env python3
"""Get / set a full-file code block in a phase guide.
  blocks.py get <phase> <File.cs> <out.cs>
  blocks.py set <phase> <File.cs> <in.cs>
  blocks.py diffall            (diff every typed block against the real file)
"""
import html, os, re, subprocess, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import phasecheck as pc


def find(n, fn):
    path = f"{pc.DOCS}/phase-{n:02d}.html"
    t = open(path, encoding="utf-8").read()
    hits = []
    for pos, kind, f, code in pc.events(n):
        if kind == "typed" and f == fn:
            hits.append(pos)
    if len(hits) != 1:
        sys.exit(f"phase {n} {fn}: {len(hits)} blocks")
    m = re.compile(r'<pre class="long"><code>(.*?)</code></pre>', re.S).match(t, hits[0])
    return path, t, m


def esc(s):
    return s.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")


if __name__ == "__main__":
    cmd = sys.argv[1]
    if cmd == "get":
        path, t, m = find(int(sys.argv[2]), sys.argv[3])
        open(sys.argv[4], "w", encoding="utf-8").write(html.unescape(m.group(1)))
    elif cmd == "set":
        path, t, m = find(int(sys.argv[2]), sys.argv[3])
        code = open(sys.argv[4], encoding="utf-8").read().rstrip("\n")
        t = t[:m.start(1)] + esc(code) + t[m.end(1):]
        open(path, "w", encoding="utf-8").write(t)
        print("updated", path, sys.argv[3])
    elif cmd == "diffall":
        for n in range(3, 11):
            for pos, kind, f, code in pc.events(n):
                if kind != "typed":
                    continue
                real = pc.REAL_FILES.get(f)
                tmp = "/tmp/_blk.cs"
                tmp = os.path.join(pc.HERE, "_blk.cs")
                open(tmp, "w").write(code.rstrip("\n") + "\n")
                r = subprocess.run(["diff", "-w", tmp, real], capture_output=True, text=True)
                ch = [l for l in r.stdout.splitlines() if l[:1] in "<>"]
                print(f"phase {n} {f}: {'SAME' if not ch else str(len(ch)) + ' diff lines'}")
