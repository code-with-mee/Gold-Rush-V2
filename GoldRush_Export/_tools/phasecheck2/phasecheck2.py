#!/usr/bin/env python3
"""Gold Rush course, NEW order (UI first): build the cumulative student script set per phase and compile it.

Usage: python3 phasecheck2.py [--manifest OUT.json] [--phases 1,2,...]
Per phase N the set = everything from phases < N, then in document order:
  * every full-file code block (contains `namespace GoldRush` + a type) -> typed file (later replaces earlier)
  * a step (<h2 id="step-k">) that mentions "Helpers/": each <Dir>/<Name>.cs or <Name>.cs in that step that
    exists in Resources/Scripts/Helpers -> imported helper
  * a step that mentions "Phases/PhaseNN/<Dir>/<Name>.cs" -> imported phase version
  * a step that mentions "Scripts/Final/" -> the Final files named there (or the whole folder if it says "folders")
The guide-derived list is cross-checked against EXPECTED (taken from _notes/v2-phase-XX-notes.md).
"""
import html, json, os, re, shutil, subprocess, sys

ROOT = "/Users/kingly/Desktop/M14/GoldRushDemo"
DOCS = f"{ROOT}/GoldRush_Export/Docs"
RES = f"{ROOT}/GoldRush_Export/Resources/Scripts"
HELP, FINAL, PHASES = f"{RES}/Helpers", f"{RES}/Final", f"{RES}/Phases"
REAL = f"{ROOT}/Assets/GoldRush/Scripts"
HERE = os.path.dirname(os.path.abspath(__file__))
RSP = os.path.join(os.path.dirname(HERE), "refs_all.rsp")
DOTNET = "/Applications/Unity/Hub/Editor/6000.6.10f1/Unity.app/Contents/Resources/Scripting/DotNetSdk"
CSC = [f"{DOTNET}/dotnet", f"{DOTNET}/sdk/8.0.318/Roslyn/bincore/csc.dll"]
TYPE_RE = re.compile(r'^\s*(?:public|internal|static|sealed|abstract|partial|\s)*(?:class|struct|enum|interface)\s+(\w+)', re.M)
BLOCK_RE = re.compile(r'<pre class="long"><code>(.*?)</code></pre>', re.S)


def cs_files(base):
    out = {}
    for d, _, fs in os.walk(base):
        for f in fs:
            if f.endswith(".cs"):
                out[os.path.relpath(os.path.join(d, f), base)] = os.path.join(d, f)
    return out


REAL_FILES = {k: v for k, v in cs_files(REAL).items() if not k.startswith(("Dev/", "Editor/"))}
HELPERS, FINALS = cs_files(HELP), cs_files(FINAL)
BYNAME = {os.path.basename(k): k for k in REAL_FILES}          # Name.cs -> Dir/Name.cs
TYPE2FILE = {}
for rel, p in REAL_FILES.items():
    for ty in TYPE_RE.findall(open(p, encoding="utf-8").read()):
        TYPE2FILE.setdefault(ty, rel)

# from _notes/v2-phase-XX-notes.md: (typed, imported) per phase. "Phases/..." = phase-version export.
EXPECTED = {
    1: ({"Core/Data.cs", "UI/MenuAction.cs", "UI/CoinFlip.cs", "UI/Bob.cs", "UI/GoldRushMenu.cs"},
        {"Core/Tween.cs", "Core/TweenRunner.cs", "Core/Fader.cs", "Core/GameAssets.cs", "Audio/AudioFX.cs", "Audio/Synth.cs",
         "UI/UIKit.cs", "UI/ButtonFX.cs", "UI/LogoTitle.cs", "UI/DayNightButton.cs",
         "World/DayNight.cs", "World/NightLight.cs", "World/EnvironmentSetup.cs"}),
    2: ({"Core/Data.cs"}, {"Phases/Phase02/UI/GoldRushMenu.cs"}),
    3: (set(), set()),
    4: ({"UI/VirtualJoystick.cs", "UI/TouchLookArea.cs", "Gameplay/PlayerMotor.cs", "Gameplay/ThirdPersonCamera.cs", "Level/PlayerSpawn.cs"}, set()),
    5: ({"Level/CoinSpot.cs", "Level/LevelScene.cs", "World/Coin.cs", "Gameplay/PlayerMotor.cs", "Gameplay/ThirdPersonCamera.cs", "Gameplay/GameManager.cs"}, set()),
    6: ({"World/ExitGate.cs", "Gameplay/GameManager.cs"}, set()),
    7: ({"Level/MonsterSpawn.cs", "Gameplay/Badge.cs", "Gameplay/GameManager.cs"},
        {"Gameplay/EnemyAI.cs", "Gameplay/CharacterFactory.cs", "Gameplay/OriginalMaterials.cs", "Core/Mats.cs", "UI/WorldMarkers.cs"}),
    8: ({"Level/PowerUpSpot.cs", "World/PowerUp.cs", "Gameplay/PlayerMotor.cs", "Gameplay/GameManager.cs"}, {"Gameplay/FX.cs"}),
}


def strip(s):
    return html.unescape(re.sub(r"<[^>]+>", " ", s))


def page(n):
    return open(f"{DOCS}/phase-{n:02d}.html", encoding="utf-8").read()


def block_file(code, before):
    types = TYPE_RE.findall(code)
    m = re.findall(r"Scripts/(\w+/\w+\.cs)", before)
    if m and os.path.basename(m[-1])[:-3] in types:
        return m[-1]
    for p in reversed(re.findall(r"\b(\w+)(?:\.cs)?\b", before[-300:])):
        if p in types and p + ".cs" in BYNAME:
            return BYNAME[p + ".cs"]
    for ty in types:
        if ty in TYPE2FILE:
            return TYPE2FILE[ty]
    return "?/" + (types[0] if types else "Unknown") + ".cs"


def events(n):
    t = page(n)
    ev = []
    for m in BLOCK_RE.finditer(t):
        code = html.unescape(m.group(1))
        if "namespace GoldRush" not in code or not TYPE_RE.search(code):
            continue  # snippet, not a whole file
        before = " ".join(strip(t[max(0, m.start() - 700):m.start()]).split())
        ev.append((m.start(), "typed", block_file(code, before), code))
    heads = [m.start() for m in re.finditer(r'<h2 id="step-\d+"', t)] + [len(t)]
    for a, b in zip(heads, heads[1:]):
        txt = strip(BLOCK_RE.sub("", t[a:b]))
        if "Helpers/" in txt:
            names = re.findall(r"\b((?:\w+/)?\w+\.cs)\b", txt)
            for name in dict.fromkeys(names):
                rel = name if name in HELPERS else next((k for k in HELPERS if os.path.basename(k) == name), None)
                if rel:
                    ev.append((a, "helper", rel, HELPERS[rel]))
        for ph in dict.fromkeys(re.findall(r"Phases/(Phase\d+/\w+/\w+\.cs)", txt)):
            p = f"{PHASES}/{ph}"
            ev.append((a, "phase", ph.split("/", 1)[1], p if os.path.exists(p) else None))
        if "Scripts/Final/" in txt:
            if "folders" in txt:
                for rel, p in FINALS.items():
                    ev.append((a, "final", rel, p))
            else:
                for name in dict.fromkeys(re.findall(r"\b((?:\w+/)?\w+\.cs)\b", txt)):
                    rel = next((k for k in FINALS if k == name or os.path.basename(k) == name), None)
                    if rel:
                        ev.append((a + 1, "final", rel, FINALS[rel]))
    ev.sort(key=lambda e: e[0])
    return ev


def build(last):
    state, snaps, found = {}, {}, {}
    for n in range(0, last + 1):
        typed, imp = set(), set()
        for pos, kind, rel, payload in events(n):
            if kind == "typed":
                state[rel] = (f"typed in phase {n}", None, payload); typed.add(rel)
            elif kind == "helper":
                state[rel] = ("Helpers/" + rel, payload, None); imp.add(rel)
            elif kind == "phase":
                state[rel] = (f"Phases/Phase{n:02d}/{rel}" + ("" if payload else " (MISSING)"), payload, None)
                imp.add(f"Phases/Phase{n:02d}/{rel}")
            elif kind == "final":
                state[rel] = ("Final/" + rel, payload, None); imp.add(rel)
        snaps[n] = dict(state)
        found[n] = (typed, imp)
    return snaps, found


def compile_set(name, files):
    d = os.path.join(HERE, "build", name)
    shutil.rmtree(d, ignore_errors=True)
    paths = []
    for rel, (src, p, code) in sorted(files.items()):
        out = os.path.join(d, rel)
        os.makedirs(os.path.dirname(out), exist_ok=True)
        if p:
            shutil.copy(p, out)
        elif code is not None:
            open(out, "w", encoding="utf-8").write(code)
        else:
            continue
        paths.append(out)
    if not paths:
        return [], []
    r = subprocess.run(CSC + ["-nologo", "-t:library", "-langversion:9", "-nostdlib", "-noconfig",
                              f"-out:{d}/out.dll", f"@{RSP}"] + paths, cwd=ROOT, capture_output=True, text=True)
    lines = sorted(set(l.replace(d + "/", "") for l in r.stdout.splitlines()))
    return [l for l in lines if ": error " in l], [l for l in lines if ": warning " in l]


def main():
    phases = list(range(1, 9))
    if "--phases" in sys.argv:
        phases = [int(x) for x in sys.argv[sys.argv.index("--phases") + 1].split(",")]
    snaps, found = build(max(phases))
    manifest, ok = {}, True
    for n in phases:
        files = snaps[n]
        typed, imp = found[n]
        et, ei = EXPECTED.get(n, (set(), set()))
        if (typed, imp) != (et, ei):
            print(f"phase {n}: guide list differs from notes: typed +{sorted(typed - et)} -{sorted(et - typed)}"
                  f" imported +{sorted(imp - ei)} -{sorted(ei - imp)}")
        manifest[str(n)] = {
            "typed": sorted(typed), "imported": sorted(imp),
            "files": [{"path": "Assets/GoldRush/Scripts/" + rel, "source": files[rel][0]} for rel in sorted(files)]}
        errs, warns = compile_set(str(n), files)
        dep = [w for w in warns if "CS0618" in w or "CS0612" in w]
        manifest[str(n)]["compile"] = {"errors": len(errs), "warnings": len(warns), "deprecated": len(dep)}
        print(f"phase {n}: {len(files):2d} files, {len(errs)} errors, {len(warns)} warnings ({len(dep)} deprecated)")
        for l in errs[:60]:
            print("    E", l)
        for l in warns:
            if not re.search(r"CS0649|CS0169|CS0414", l):   # unassigned [SerializeField] fields: normal in Unity
                print("    W", l)
        ok &= not errs
    if "--manifest" in sys.argv:
        out = sys.argv[sys.argv.index("--manifest") + 1]
        json.dump(manifest, open(out, "w"), indent=1)
        print("manifest ->", out)
    sys.exit(0 if ok else 1)


if __name__ == "__main__":
    main()
