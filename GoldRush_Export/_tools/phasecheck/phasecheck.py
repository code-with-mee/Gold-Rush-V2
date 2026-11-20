#!/usr/bin/env python3
"""Build the cumulative student script set per phase from the Gold Rush guides and compile it.

Usage: python3 phasecheck.py [--manifest OUT.json] [--keep]
For phase N = 3..10 the set is: every full-file code block typed in phases <= N
(later block of the same file replaces the earlier one), plus every helper the guides
tell students to import (any step that mentions "Helpers/": each *.cs name in that step
that exists in Resources/Scripts/Helpers), plus, in a step that mentions
"Resources/Scripts/Final/", the whole Final folder. Events are applied in document order.
Phase "10-pre" = phase 10 before the Final copy step.
"""
import html, json, os, re, shutil, subprocess, sys

ROOT = "/Users/kingly/Desktop/M14/GoldRushDemo"
DOCS = f"{ROOT}/GoldRush_Export/Docs"
HELP = f"{ROOT}/GoldRush_Export/Resources/Scripts/Helpers"
FINAL = f"{ROOT}/GoldRush_Export/Resources/Scripts/Final"
REAL = f"{ROOT}/Assets/GoldRush/Scripts"
HERE = os.path.dirname(os.path.abspath(__file__))
RSP = os.path.join(os.path.dirname(HERE), "refs_all.rsp")
DOTNET = "/Applications/Unity/Hub/Editor/6000.6.10f1/Unity.app/Contents/Resources/Scripting/DotNetSdk"
CSC = [f"{DOTNET}/dotnet", f"{DOTNET}/sdk/8.0.318/Roslyn/bincore/csc.dll"]

TYPE_RE = re.compile(r'^\s*(?:public|internal|static|sealed|abstract|partial|\s)*(?:class|struct|enum|interface)\s+(\w+)', re.M)


def cs_files(base):
    out = {}
    for d, _, fs in os.walk(base):
        for f in fs:
            if f.endswith(".cs"):
                out[f] = os.path.join(d, f)
    return out


REAL_FILES = cs_files(REAL)
HELPERS = cs_files(HELP)
FINALS = cs_files(FINAL)
# type name -> real file name (first top-level-ish declaration wins)
TYPE2FILE = {}
for fn, p in REAL_FILES.items():
    for t in TYPE_RE.findall(open(p, encoding="utf-8").read()):
        TYPE2FILE.setdefault(t, fn)


def rel_real(fn):
    p = REAL_FILES.get(fn)
    return "Assets/GoldRush/Scripts/" + os.path.relpath(p, REAL) if p else "Assets/GoldRush/Scripts/?/" + fn


def strip(s):
    return html.unescape(re.sub(r"<[^>]+>", " ", s))


def events(n):
    """Return ordered list of (pos, kind, filename, payload) for phase n."""
    t = open(f"{DOCS}/phase-{n:02d}.html", encoding="utf-8").read()
    ev = []
    blocks = list(re.finditer(r'<pre class="long"><code>(.*?)</code></pre>', t, re.S))
    for m in blocks:
        code = html.unescape(m.group(1))
        if not code.lstrip().startswith("using"):
            continue  # snippet, not a whole file
        before = strip(t[max(0, m.start() - 600):m.start()])
        paths = re.findall(r"(\w+)\.cs\b", before)
        fn = None
        types = TYPE_RE.findall(code)
        # prefer a file named right before the block that matches a declared type
        for p in reversed(paths):
            if p in types or TYPE2FILE.get(types[0] if types else "") == p + ".cs":
                fn = p + ".cs"; break
        if not fn:
            for ty in types:
                if ty in TYPE2FILE:
                    fn = TYPE2FILE[ty]; break
        if not fn:
            fn = (types[0] if types else "Unknown") + ".cs"
        ev.append((m.start(), "typed", fn, code))
    # steps
    heads = [m.start() for m in re.finditer(r'<h2 id="step-\d+"', t)] + [len(t)]
    for a, b in zip(heads, heads[1:]):
        sec = re.sub(r'<pre class="long"><code>.*?</code></pre>', "", t[a:b], flags=re.S)
        txt = strip(sec)
        if re.search(r"Scripts/Final/", txt):
            if "folders" in txt:
                ev.append((a, "final", None, None))           # copy the whole Final folder
            else:
                for name in dict.fromkeys(re.findall(r"(\w+\.cs)\b", txt)):
                    if name in FINALS:
                        ev.append((a + 1, "finalfile", name, None))
        if "Helpers/" in txt:
            for name in dict.fromkeys(re.findall(r"(\w+\.cs)\b", txt)):
                if name in HELPERS:
                    ev.append((a, "helper", name, None))
    ev.sort(key=lambda e: e[0])
    return ev


def build():
    state = {}  # filename -> (source label, content-path or code)
    snapshots = {}
    for n in range(0, 11):
        for pos, kind, fn, code in events(n):
            if kind == "final":
                if n == 10:
                    snapshots["10-pre"] = dict(state)
                for f, p in FINALS.items():
                    state[f] = ("Final/" + os.path.relpath(p, FINAL), p, None)
            elif kind == "finalfile":
                state[fn] = ("Final/" + os.path.relpath(FINALS[fn], FINAL), FINALS[fn], None)
            elif kind == "helper":
                state[fn] = ("Helpers/" + os.path.relpath(HELPERS[fn], HELP), HELPERS[fn], None)
            else:
                state[fn] = (f"typed in phase {n}", None, code)
        snapshots[str(n)] = dict(state)
    return snapshots


def compile_set(name, files):
    d = os.path.join(HERE, "build", name)
    shutil.rmtree(d, ignore_errors=True)
    os.makedirs(d)
    paths = []
    for fn, (src, p, code) in sorted(files.items()):
        out = os.path.join(d, fn)
        if p:
            shutil.copy(p, out)
        else:
            open(out, "w", encoding="utf-8").write(code)
        paths.append(out)
    if not paths:
        return [], []
    r = subprocess.run(CSC + ["-nologo", "-t:library", "-langversion:9", "-nostdlib", "-noconfig",
                              f"-out:{d}/out.dll", f"@{RSP}"] + paths,
                       cwd=ROOT, capture_output=True, text=True)
    lines = sorted(set(l.replace(d + "/", "") for l in r.stdout.splitlines()))
    errs = [l for l in lines if ": error " in l]
    warns = [l for l in lines if ": warning " in l]
    return errs, warns


def main():
    snaps = build()
    order = ["3", "4", "5", "6", "7", "8", "9", "10-pre", "10"]
    manifest = {}
    ok = True
    for k in order:
        files = snaps[k]
        manifest[k] = [{"path": rel_real(fn), "source": files[fn][0]} for fn in sorted(files, key=rel_real)]
        errs, warns = compile_set(k, files)
        dep = [w for w in warns if "CS0618" in w or "CS0612" in w]
        print(f"phase {k:>6}: {len(files):2d} files, {len(errs)} errors, {len(warns)} warnings ({len(dep)} deprecated)")
        for l in errs[:60]:
            print("    E", l)
        for l in warns:
            print("    W", l)
        ok &= not errs
    if "--manifest" in sys.argv:
        out = sys.argv[sys.argv.index("--manifest") + 1]
        json.dump(manifest, open(out, "w"), indent=1)
        print("manifest ->", out)
    sys.exit(0 if ok else 1)


if __name__ == "__main__":
    main()
