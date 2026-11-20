import re, os, sys, subprocess
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import phasecheck2 as pc
snaps, found = pc.build(8)
def content(entry):
    src, p, code = entry
    return open(p).read() if p else code
def strip_comments(s):
    s = re.sub(r'/\*.*?\*/', '', s, flags=re.S); return re.sub(r'//[^\n]*', '', s)
FIELD_RE = re.compile(r'^\s*((?:\[[^\]]*\]\s*)*)((?:public|private|protected|internal|static|readonly|const|\s)*)\s*([\w<>\[\],\. ?]+?)\s+([\w\s,=\.\(\)\-\+\*fF"<>\[\]{}:]+?);', re.M)
def serialized(src):
    s = strip_comments(src)
    out = {}
    # crude: only class-level lines: lines with 8 spaces indent inside namespace
    for line in s.splitlines():
        m = re.match(r'^ {8}((?:\[[^\]]*\]\s*)*)(.*?);\s*$', line)
        if not m: continue
        attrs, decl = m.group(1), m.group(2)
        if '(' in decl.split('=')[0] or ' => ' in decl or decl.startswith(('return','using')): continue
        mods = decl.split()
        if any(k in mods for k in ('static','const','readonly')): continue
        if 'NonSerialized' in attrs: continue
        pub = decl.startswith('public ')
        if not (pub or 'SerializeField' in attrs): continue
        if '{' in decl: continue  # property
        body = re.sub(r'^(public|private|protected|internal)\s+', '', decl)
        # type then names
        mm = re.match(r'([\w<>\[\],\.]+(?:<[^>]*>)?)\s+(.*)$', body)
        if not mm: continue
        typ, names = mm.groups()
        for part in re.split(r',(?![^<(]*[>)])', names):
            n = part.split('=')[0].strip()
            if re.match(r'^\w+$', n): out[n] = typ
    return out
real = {rel: open(p).read() for rel, p in pc.REAL_FILES.items()}
# versions per file
vers = {}
for n in range(1, 9):
    for rel, e in snaps[n].items():
        c = content(e)
        if rel not in vers or vers[rel][-1][1] != c:
            vers.setdefault(rel, []).append((n, c, e[0]))
for rel, vl in sorted(vers.items()):
    rf = serialized(real.get(rel, ''))
    for n, c, src in vl:
        f = serialized(c)
        miss = {k: v for k, v in f.items() if k not in rf}
        typ = {k: (v, rf[k]) for k, v in f.items() if k in rf and rf[k] != v}
        lost = [k for k in rf if k not in f]
        same = c.strip() == real.get(rel, '').strip()
        print(f"{rel:32s} p{n} [{src}] {'== REAL' if same else ''} fields={len(f)}", end='')
        if miss: print(f"  NOT-IN-REAL={miss}", end='')
        if typ: print(f"  TYPE-DIFF={typ}", end='')
        if lost and not same: print(f"  real-only={lost}", end='')
        print()
