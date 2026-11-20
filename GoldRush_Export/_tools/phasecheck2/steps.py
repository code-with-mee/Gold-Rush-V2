# compile after every step (cumulative) to find transient errors inside a phase
import sys, re, os; sys.path.insert(0, '.')
import phasecheck2 as pc
state = {}
for n in range(1, 9):
    t = pc.page(n)
    heads = [(m.start(), m.group(1)) for m in re.finditer(r'<h2 id="step-(\d+)"', t)]
    def step_of(pos):
        s = None
        for p, k in heads:
            if p <= pos: s = k
        return s
    evs = pc.events(n)
    seen = {}
    for e in evs: seen.setdefault(step_of(e[0]), []).append(e)
    for st, el in seen.items():
        changed = []
        for pos, kind, rel, payload in el:
            if kind == "typed": state[rel] = ("t", None, payload)
            else: state[rel] = (kind, payload, None)
            changed.append(rel)
        errs, _ = pc.compile_set(f"s{n}_{st}", state)
        kinds = sorted(set(re.sub(r'^([^(]+)\(.*error (CS\d+): (.*)$', r'\1 \2 \3', l) for l in errs))
        print(f"phase {n} step {st}: +{sorted(set(changed))} -> {len(errs)} errors")
        for k in kinds[:8]: print("     ", k[:200])
