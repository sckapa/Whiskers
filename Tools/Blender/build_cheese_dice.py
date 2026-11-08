"""Whiskers Rig-ma-roll: builds the nine cheese dice ("The Lucky Nine").

Spec: Design/CheeseDice_Blender_Spec.md

Run headless from the repo root:
    blender -b -P Tools/Blender/build_cheese_dice.py
    blender -b -P Tools/Blender/build_cheese_dice.py -- --preview <dir>   # also renders preview sheets

Outputs:
    Assets/Whiskers/Models/Dice/Die_<Cheese>_<die>.fbx and ..._Lucky9.fbx  (18 files)
    Art/Blender/CheeseDice.blend

Every random choice is seeded from the cheese name, so rebuilding gives identical meshes.
Numbers use the game's 3x5 pixel font (the same glyphs as DiceSpriteFactory.cs).
"""

import bpy
import bmesh
import math
import os
import random
import sys
import time
import zlib
from mathutils import Vector, Matrix

# ---------------------------------------------------------------- paths / args

try:
    HERE = os.path.dirname(os.path.abspath(__file__))
    ROOT = os.path.normpath(os.path.join(HERE, "..", ".."))
except NameError:
    ROOT = os.getcwd()

FBX_DIR = os.path.join(ROOT, "Assets", "Whiskers", "Models", "Dice")
BLEND_PATH = os.path.join(ROOT, "Art", "Blender", "CheeseDice.blend")

ARGV = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
PREVIEW_DIR = ARGV[ARGV.index("--preview") + 1] if "--preview" in ARGV else None

# ---------------------------------------------------------------- constants

D6_BEVEL = 0.06
POLY_BEVEL = 0.04
BRIE_RIND_BEVEL = 0.10      # the soft, rounded rind edge on the Brie wedge
ENGRAVE = 0.03              # number depth
IMPRINT = 0.004             # painted flecks are cut this deep, then pushed back flush
PIP_SPACING = 0.25          # d6 pip grid offset from face centre (edge = 1.0)
CLEAR = 0.015               # breathing room between holes, numbers and bevels

MAIN, HOLES, RIND = 0, 1, 2

# The game's 3x5 digit font (rows top to bottom), copied from DiceSpriteFactory.Font.
FONT = [
    ("111", "101", "101", "101", "111"), ("010", "110", "010", "010", "111"),
    ("111", "001", "111", "100", "111"), ("111", "001", "011", "001", "111"),
    ("101", "101", "111", "001", "001"), ("111", "100", "111", "001", "111"),
    ("111", "100", "111", "101", "111"), ("111", "001", "010", "010", "010"),
    ("111", "101", "111", "101", "111"), ("111", "101", "111", "001", "111"),
]

PIPS = {
    1: [(0, 0)],
    2: [(-1, -1), (1, 1)],
    3: [(-1, -1), (0, 0), (1, 1)],
    4: [(-1, -1), (1, -1), (-1, 1), (1, 1)],
    5: [(-1, -1), (1, -1), (0, 0), (-1, 1), (1, 1)],
    6: [(-1, -1), (-1, 0), (-1, 1), (1, -1), (1, 0), (1, 1)],
    9: [(x, y) for y in (1, 0, -1) for x in (-1, 0, 1)],
}

# hole styles: sphere offset (centre sits off*r outside the face, so the cut is (1-off)*r deep)
STYLE_OFFSET = {"sphere": 0.5, "shallow": 0.7, "pin": 0.5, "rough": 0.4}

CHEESES = [
    dict(name="Emmental", label="Emmental", die="d6", main="#FFE74A", holes="#DE9C21", rind=None,
         pip="sphere", pip_r=0.11, extra="sphere", extra_r=(0.045, 0.065)),
    dict(name="Cheddar", label="Mature Cheddar", die="d6", main="#FFA521", holes="#C66310", rind=None,
         pip="rough", pip_r=0.08, extra="rough", extra_r=(0.035, 0.05), chipped=True),
    dict(name="RedWindsor", label="Red Windsor", die="d6", main="#F7C663", holes="#B52131", rind=None,
         pip="paint", pip_r=0.085, extra="paint", extra_r=(0.03, 0.045), streaks=4),
    dict(name="Brie", label="Brie", die="d4", main="#FFEFA5", holes="#D6B563", rind="#F7F7DE",
         extra="shallow", extra_r=(0.03, 0.045), soft=True),
    dict(name="Parmesan", label="Parmesan", die="d4", main="#F7D67B", holes="#C69442", rind="#AD6B29",
         extra="rough", extra_r=(0.03, 0.045)),
    dict(name="Pecorino", label="Pecorino", die="d8", main="#EFDEAD", holes="#B59C63", rind="#845229",
         extra="pin", extra_r=(0.022, 0.03), basket=True),
    dict(name="BlueCheese", label="Blue Cheese", die="d10", main="#5A94F7", holes="#294AB5", rind=None,
         extra="sphere", extra_r=(0.045, 0.07), streaks=3, soft=True),
    dict(name="Gouda", label="Gouda", die="d12", main="#FFC631", holes="#CE8418", rind="#FFEF4A",
         extra="sphere", extra_r=(0.04, 0.06), wax=True),
    dict(name="Edam", label="Edam", die="d20", main="#FFDE6B", holes="#D6A539", rind="#DE1839",
         extra="sphere", extra_r=(0.04, 0.06), wax=True),
]

SIDES = {"d4": 4, "d6": 6, "d8": 8, "d10": 10, "d12": 12, "d20": 20}


def seeded(*parts):
    return random.Random(zlib.crc32(":".join(str(p) for p in parts).encode()))


# ---------------------------------------------------------------- materials

def srgb_hex_to_linear(h):
    h = h.lstrip("#")
    out = []
    for i in (0, 2, 4):
        c = int(h[i:i + 2], 16) / 255.0
        out.append(c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4)
    return (out[0], out[1], out[2], 1.0)


def make_material(name, hexc, soft):
    m = bpy.data.materials.new(name)
    try:
        m.use_nodes = True
    except Exception:
        pass
    col = srgb_hex_to_linear(hexc)
    m.diffuse_color = col
    m.roughness = 0.65
    m.specular_intensity = 0.3
    nt = m.node_tree
    if nt is not None:
        bsdf = next((n for n in nt.nodes if n.type == "BSDF_PRINCIPLED"), None)
        if bsdf is None:
            bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
            out = next((n for n in nt.nodes if n.type == "OUTPUT_MATERIAL"), None) \
                or nt.nodes.new("ShaderNodeOutputMaterial")
            nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])

        def setin(names, val):
            for nm in names:
                if nm in bsdf.inputs:
                    bsdf.inputs[nm].default_value = val
                    return

        setin(["Base Color"], col)
        setin(["Roughness"], 0.65)
        setin(["Specular IOR Level", "Specular"], 0.3)
        setin(["Subsurface Weight", "Subsurface"], 0.05 if soft else 0.0)
    return m


# ---------------------------------------------------------------- 2D helpers (face-local)

def inner_dist(pt, poly2):
    """Distance from pt to the nearest edge of a CCW convex polygon; negative when outside."""
    m = 1e9
    n = len(poly2)
    for i in range(n):
        ax, ay = poly2[i]
        bx, by = poly2[(i + 1) % n]
        ex, ey = bx - ax, by - ay
        L = math.hypot(ex, ey)
        if L < 1e-9:
            continue
        m = min(m, (ex * (pt[1] - ay) - ey * (pt[0] - ax)) / L)
    return m


def cheb_center(poly2):
    xs = [p[0] for p in poly2]
    ys = [p[1] for p in poly2]
    x0, x1, y0, y1 = min(xs), max(xs), min(ys), max(ys)
    best = (-1e9, 0.0, 0.0)
    for _ in range(4):
        N = 24
        for i in range(N + 1):
            for j in range(N + 1):
                x = x0 + (x1 - x0) * i / N
                y = y0 + (y1 - y0) * j / N
                d = inner_dist((x, y), poly2)
                if d > best[0] + 1e-9:
                    best = (d, x, y)
        w = (x1 - x0) / N * 2
        h = (y1 - y0) / N * 2
        x0, x1, y0, y1 = best[1] - w, best[1] + w, best[2] - h, best[2] + h
    return best


class Face:
    """One flat face of the base die, in final (scaled, centred) coordinates."""

    def __init__(self, center, normal, poly3, kind, role, tag, margin):
        n = normal.normalized()
        ref = Vector((0, 1, 0)) if abs(n.z) > 0.9 else Vector((0, 0, 1))
        ref = (ref - n * ref.dot(n)).normalized()
        if kind == "snap":
            dirs = [(p - center - n * (p - center).dot(n)).normalized() for p in poly3]
            up = max(dirs, key=lambda d: d.dot(ref))
        elif kind == "kite":
            far = max(poly3, key=lambda p: (p - center).length)
            up = (far - center)
            up = (up - n * up.dot(n)).normalized()
        else:
            up = ref
        right = up.cross(n).normalized()
        up = n.cross(right).normalized()
        poly2 = [((p - center).dot(right), (p - center).dot(up)) for p in poly3]
        rin, cx, cy = cheb_center(poly2)
        self.n, self.right, self.up = n, right, up
        self.c = center + right * cx + up * cy
        self.poly2 = [(x - cx, y - cy) for x, y in poly2]
        self.rin = rin
        self.role = role
        self.tag = tag
        self.margin = margin
        self.value = None
        self.is_top = n.z > 0.999
        self.allow_extras = role == "main"
        self.obstacles = []   # ('c', u, v, r) or ('b', u, v, hu, hv) in face-local coords

    def world(self, u, v, h=0.0):
        return self.c + self.right * u + self.up * v + self.n * h

    def clear_of(self, u, v, r):
        for o in self.obstacles:
            if o[0] == "c":
                if math.hypot(u - o[1], v - o[2]) < r + o[3] + CLEAR:
                    return False
            else:
                if abs(u - o[1]) < r + o[3] + CLEAR and abs(v - o[2]) < r + o[4] + CLEAR:
                    return False
        return True

    def fits(self, u, v, r):
        return inner_dist((u, v), self.poly2) >= r + self.margin + CLEAR


# ---------------------------------------------------------------- base shapes

PHI = (1 + 5 ** 0.5) / 2


def poly_points(die):
    if die == "d8":
        return [(1, 0, 0), (-1, 0, 0), (0, 1, 0), (0, -1, 0), (0, 0, 1), (0, 0, -1)]
    if die == "d20":
        pts = []
        for a in (-1, 1):
            for b in (-PHI, PHI):
                pts += [(0, a, b), (a, b, 0), (b, 0, a)]
        return pts
    if die == "d12":
        pts = [(x, y, z) for x in (-1, 1) for y in (-1, 1) for z in (-1, 1)]
        for a in (-1 / PHI, 1 / PHI):
            for b in (-PHI, PHI):
                pts += [(0, a, b), (a, b, 0), (b, 0, a)]
        return pts
    if die == "d10":
        # pentagonal trapezohedron; ring height chosen so every kite is planar
        h = 1.0
        c = math.cos(math.radians(36))
        z0 = h * (1 - c) / (1 + c)
        pts = [(0, 0, h), (0, 0, -h)]
        for k in range(5):
            a = math.radians(72 * k)
            pts.append((math.cos(a), math.sin(a), z0))
            a2 = a + math.radians(36)
            pts.append((math.cos(a2), math.sin(a2), -z0))
        return pts
    raise ValueError(die)


def fib_dirs(n=400):
    out = []
    ga = math.pi * (3 - math.sqrt(5))
    for i in range(n):
        z = 1 - 2 * (i + 0.5) / n
        r = math.sqrt(max(0.0, 1 - z * z))
        out.append(Vector((r * math.cos(ga * i), r * math.sin(ga * i), z)))
    return out


DIRS = fib_dirs()


def mean_width(pts):
    """Average caliper width over all view directions: how big a shape reads on screen."""
    return sum(max(p.dot(d) for p in pts) - min(p.dot(d) for p in pts) for d in DIRS) / len(DIRS)


D6_MEAN_WIDTH = mean_width([Vector((x, y, z)) for x in (-.5, .5) for y in (-.5, .5) for z in (-.5, .5)])


def volume_centroid(bm):
    vol = 0.0
    acc = Vector()
    for f in bm.faces:
        vs = [v.co for v in f.verts]
        for i in range(1, len(vs) - 1):
            a, b, c = vs[0], vs[i], vs[i + 1]
            v6 = a.dot(b.cross(c))
            vol += v6
            acc += v6 * (a + b + c) / 4.0
    return acc / vol


def build_wedge(bm, ch, tag):
    """Triangular cheese wedge (a triangular prism): apex towards -Y, rind at the back (+Y)."""
    brie = ch["name"] == "Brie"
    band = ch["name"] == "Parmesan"
    H = 0.72
    zb, zt = -H / 2, H / 2
    zm = zb + 0.2 * H
    apex, bl, br = (0.0, -0.85), (-0.62, 0.5), (0.62, 0.5)
    sag = 0.08 if brie else 0.0

    def V(xy, z):
        return bm.verts.new((xy[0], xy[1], z))

    A0, L0, R0 = V(apex, zb), V(bl, zb), V(br, zb)
    A1, L1, R1 = V(apex, zt - sag), V(bl, zt), V(br, zt)
    rind_ok = ch["rind"] is not None

    def F(verts, t, mat):
        f = bm.faces.new(verts)
        f[tag] = t
        f.material_index = mat
        return f

    F([A0, R0, L0], 2, RIND if rind_ok else MAIN)     # base
    F([A1, L1, R1], 1, MAIN)                          # top: the big cut face
    if band:
        Am, Lm, Rm = V(apex, zm), V(bl, zm), V(br, zm)
        F([A0, L0, Lm, Am], 6, RIND)
        F([Am, Lm, L1, A1], 3, MAIN)
        F([R0, A0, Am, Rm], 7, RIND)
        F([Rm, Am, A1, R1], 4, MAIN)
        F([L0, R0, Rm, R1, L1, Lm], 5, RIND)
    else:
        F([A0, L0, L1, A1], 3, MAIN)
        F([R0, A0, A1, R1], 4, MAIN)
        F([L0, R0, R1, L1], 5, RIND if rind_ok else MAIN)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))


def mesh_object(name, bm, mats, coll):
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    for m in mats:
        me.materials.append(m)
    obj = bpy.data.objects.new(name, me)
    coll.objects.link(obj)
    return obj


def apply_modifiers(obj):
    bpy.context.view_layer.update()
    dg = bpy.context.evaluated_depsgraph_get()
    ev = obj.evaluated_get(dg)
    me = bpy.data.meshes.new_from_object(ev, preserve_all_data_layers=True, depsgraph=dg)
    old = obj.data
    obj.modifiers.clear()
    obj.data = me
    bpy.data.meshes.remove(old)


def build_base(ch, mats, coll):
    die = ch["die"]
    name = ch["name"]
    bm = bmesh.new()
    tag = bm.faces.layers.int.new("tag")

    if die == "d6":
        bmesh.ops.create_cube(bm, size=1.0)
        kind, bevel = "ref", D6_BEVEL
    elif die == "d4":
        build_wedge(bm, ch, tag)
        kind, bevel = "ref", POLY_BEVEL
    else:
        vs = [bm.verts.new(p) for p in poly_points(die)]
        bmesh.ops.convex_hull(bm, input=vs)
        bmesh.ops.dissolve_limit(bm, angle_limit=math.radians(1.0),
                                 verts=list(bm.verts), edges=list(bm.edges))
        bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
        bm.normal_update()
        topf = max(bm.faces, key=lambda f: f.normal.z)
        rot = topf.normal.rotation_difference(Vector((0, 0, 1))).to_matrix().to_4x4()
        bmesh.ops.transform(bm, matrix=rot, verts=list(bm.verts))
        kind = "kite" if die == "d10" else "snap"
        bevel = POLY_BEVEL

    if die != "d6":
        target = D6_MEAN_WIDTH * (1.15 if die in ("d12", "d20") else 1.0)
        s = target / mean_width([v.co for v in bm.verts])
        bmesh.ops.scale(bm, vec=(s, s, s), verts=list(bm.verts))
    com = volume_centroid(bm)
    bmesh.ops.translate(bm, vec=-com, verts=list(bm.verts))
    bm.normal_update()

    if ch.get("basket"):
        # Pecorino: basket-pressed rind on the faces that form the outline when it sits on a face
        for f in bm.faces:
            if abs(f.normal.z) < 0.99:
                f.material_index = RIND

    margin = BRIE_RIND_BEVEL if name == "Brie" else bevel
    faces = []
    for f in bm.faces:
        role = "rind" if f.material_index == RIND else "main"
        face = Face(f.calc_center_median(), f.normal.copy(), [v.co.copy() for v in f.verts],
                    kind, role, f[tag], margin)
        if die == "d4":
            face.is_top = f[tag] == 1     # the Brie top droops, so go by tag, not by normal
        faces.append(face)

    if name == "Brie":
        back = next(f for f in bm.faces if f[tag] == 5)
        edges = list(back.edges)
        verts = sorted({v for e in edges for v in e.verts}, key=lambda v: v.index)
        bmesh.ops.bevel(bm, geom=edges + verts, offset=BRIE_RIND_BEVEL, offset_type="OFFSET",
                        segments=4, profile=0.5, affect="EDGES", material=RIND, clamp_overlap=True)

    obj = mesh_object(f"Base_{name}", bm, mats, coll)
    bm.free()

    mod = obj.modifiers.new("Bevel", "BEVEL")
    mod.width = bevel
    mod.segments = 2
    mod.limit_method = "ANGLE"
    mod.angle_limit = math.radians(30)
    mod.material = RIND if ch.get("wax") else -1
    apply_modifiers(obj)

    if ch.get("chipped"):
        chip_bevels(obj, seeded(name, "chips"))

    assign_values(ch, faces)
    return obj, faces


def chip_bevels(obj, rng):
    """Cheddar: knock random bits off the bevel ring; the flat faces stay flat."""
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bm.normal_update()
    flat = set()
    for f in bm.faces:
        n = f.normal
        if max(abs(n.x), abs(n.y), abs(n.z)) > 0.999:
            flat.update(f.verts)
    cand = [v for v in bm.verts if v not in flat]
    cand_set = set(cand)
    depth = {v: rng.uniform(0.0, 0.006) for v in cand}       # slight unevenness everywhere
    for i in rng.sample(range(len(cand)), k=min(14, len(cand))):
        v = cand[i]
        d = rng.uniform(0.03, 0.05)                           # a few real chips
        depth[v] = max(depth[v], d)
        for e in v.link_edges:
            nb = e.other_vert(v)
            if nb in cand_set:
                depth[nb] = max(depth[nb], d * 0.5)
    for v in cand:
        v.co -= v.normal * depth[v]
    bm.to_mesh(obj.data)
    bm.free()


def assign_values(ch, faces):
    die = ch["die"]
    n = SIDES[die]
    if die == "d6":
        table = {(0, 0, 1): 1, (0, 0, -1): 6, (0, -1, 0): 2, (0, 1, 0): 5, (1, 0, 0): 3, (-1, 0, 0): 4}
        for f in faces:
            key = tuple(int(round(c)) for c in f.n)
            f.value = table[key]
        return
    if die == "d4":
        table = {1: 4, 2: 1, 3: 2, 4: 3}      # top, base, left, right; the rind back is blank
        for f in faces:
            f.value = table.get(f.tag)
        return
    top = next(f for f in faces if f.is_top)
    rest = [f for f in faces if f is not top]
    bottom = min(rest, key=lambda f: f.n.dot(top.n))
    top.value, bottom.value = n, 1
    rest = [f for f in rest if f is not bottom]
    pairs, used = [], set()
    for f in rest:
        if id(f) in used:
            continue
        o = min((g for g in rest if g is not f and id(g) not in used), key=lambda g: g.n.dot(f.n))
        used |= {id(f), id(o)}
        hi_f, lo_f = (f, o) if f.c.z >= o.c.z else (o, f)
        pairs.append((hi_f, lo_f))
    pairs.sort(key=lambda p: math.atan2(p[0].c.y, p[0].c.x))
    for i, (a, b) in enumerate(pairs):
        lo, hi = 2 + i, n - 1 - i                 # opposite faces add up to n + 1
        a.value, b.value = (hi, lo) if i % 2 == 0 else (lo, hi)


# ---------------------------------------------------------------- cutter geometry

def add_sphere(bm, pos, r, mat, segs=20, rings=10):
    ret = bmesh.ops.create_uvsphere(bm, u_segments=segs, v_segments=rings, radius=r,
                                    matrix=Matrix.Translation(pos))
    for face in {fc for v in ret["verts"] for fc in v.link_faces}:
        face.material_index = mat


def add_rough(bm, pos, r, mat, rng):
    ret = bmesh.ops.create_icosphere(bm, subdivisions=2, radius=r, matrix=Matrix.Translation(pos))
    for v in ret["verts"]:
        v.co = pos + (v.co - pos) * rng.uniform(0.7, 1.2)
    for face in {fc for v in ret["verts"] for fc in v.link_faces}:
        face.material_index = mat


def add_prism(bm, f, pts2, h0, h1, mat):
    bot = [bm.verts.new(f.world(u, v, h0)) for u, v in pts2]
    top = [bm.verts.new(f.world(u, v, h1)) for u, v in pts2]
    bm.faces.new(list(reversed(bot))).material_index = mat
    bm.faces.new(top).material_index = mat
    n = len(pts2)
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new([bot[i], bot[j], top[j], top[i]]).material_index = mat


def blob(rng, u, v, r, n=14):
    """A wobbly star-shaped fleck outline (always a simple polygon)."""
    waves = [(rng.uniform(0.05, 0.14), rng.uniform(0, 2 * math.pi)) for _ in range(3)]
    pts = []
    for i in range(n):
        t = 2 * math.pi * i / n
        rr = r * (1 + sum(a * math.sin((k + 2) * t + p) for k, (a, p) in enumerate(waves)))
        pts.append((u + rr * math.cos(t), v + rr * math.sin(t)))
    return pts


def ribbon(rng, u0, v0, ang, length, width):
    """A tapered, gently curving streak (marbling / blue vein)."""
    bend = rng.uniform(-0.25, 0.25)
    wav, ph = rng.uniform(0.02, 0.05), rng.uniform(0, 2 * math.pi)
    dx, dy = math.cos(ang), math.sin(ang)
    px, py = -dy, dx
    N = 12
    centre = []
    for i in range(N + 1):
        t = i / N
        off = length * (bend * math.sin(math.pi * t)) + wav * math.sin(2 * math.pi * t * 1.5 + ph)
        centre.append((u0 + dx * length * t + px * off, v0 + dy * length * t + py * off))
    left, right = [], []
    for i, (cx, cy) in enumerate(centre):
        a = centre[max(0, i - 1)]
        b = centre[min(N, i + 1)]
        tx, ty = b[0] - a[0], b[1] - a[1]
        L = math.hypot(tx, ty) or 1.0
        nx, ny = -ty / L, tx / L
        w = width * (0.3 + 0.7 * math.sin(math.pi * i / N)) / 2
        left.append((cx + nx * w, cy + ny * w))
        right.append((cx - nx * w, cy - ny * w))
    return right + list(reversed(left)), centre


def number_size(text, p):
    k = len(text)
    return (4 * k - 1) * p, 5 * p


def add_number(bm, f, text, p, mat):
    """Pixel-font number as one clean closed solid per connected stroke (no overlapping boxes,
    which leave float slivers after the boolean on tilted faces)."""
    W, H = number_size(text, p)
    on = set()
    for j, ch in enumerate(text):
        g = FONT[int(ch)]
        for r in range(5):
            for c in range(3):
                if g[r][c] == "1":
                    on.add((4 * j + c, 4 - r))           # grid x right, grid y up
    comp = {}
    for px in sorted(on):
        if px in comp:
            continue
        cid = len(set(comp.values()))
        comp[px] = cid
        stack = [px]
        while stack:
            x, y = stack.pop()
            for nb in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                if nb in on and nb not in comp:
                    comp[nb] = cid
                    stack.append(nb)
    verts = {}

    def V(gx, gy, layer, k):
        key = (gx, gy, layer, k)
        if key not in verts:
            verts[key] = bm.verts.new(f.world(-W / 2 + gx * p, -H / 2 + gy * p, (-ENGRAVE, 0.04)[layer]))
        return verts[key]

    for x, y in sorted(on):
        k = comp[(x, y)]
        b = [V(x, y, 0, k), V(x + 1, y, 0, k), V(x + 1, y + 1, 0, k), V(x, y + 1, 0, k)]
        t = [V(x, y, 1, k), V(x + 1, y, 1, k), V(x + 1, y + 1, 1, k), V(x, y + 1, 1, k)]
        quads = [list(reversed(b)), t]
        if (x, y - 1) not in on:
            quads.append([b[0], b[1], t[1], t[0]])
        if (x + 1, y) not in on:
            quads.append([b[1], b[2], t[2], t[1]])
        if (x, y + 1) not in on:
            quads.append([b[2], b[3], t[3], t[2]])
        if (x - 1, y) not in on:
            quads.append([b[3], b[0], t[0], t[3]])
        for q in quads:
            bm.faces.new(q).material_index = mat


# ---------------------------------------------------------------- per-cheese layout

def face_texts(ch, f):
    """Every string this face shows across both variants (so shared extras avoid all of them)."""
    if f.value is None or ch["die"] == "d6":
        return []
    texts = [str(f.value)]
    if f.is_top:
        texts.append("9")
    return texts


def pip_points(value):
    return [(x * PIP_SPACING, y * PIP_SPACING) for x, y in PIPS[value]]


def pip_open_radius(ch):
    r = ch["pip_r"]
    if ch["pip"] == "paint":
        return r * 1.25
    if ch["pip"] == "rough":
        return r * 1.15
    off = STYLE_OFFSET[ch["pip"]]
    return r * math.sqrt(1 - off * off)


def plan_layout(ch, faces):
    """Seeded decisions shared by both variants: number size, extra holes, streaks, basket dimples."""
    name = ch["name"]
    rng = seeded(name, "layout")
    plan = dict(p=None, extras=[], streaks=[], dimples=[])

    # number pixel size: one size per die, the largest that fits every face
    if ch["die"] != "d6":
        best = 1e9
        for f in faces:
            for t in face_texts(ch, f):
                k = len(t)
                half = math.hypot((4 * k - 1) / 2.0, 2.5) + 0.3
                best = min(best, (f.rin - f.margin - CLEAR) / half)
        plan["p"] = best * 0.9

    # obstacles: numbers / pips on every face (union of both variants on the top face)
    for f in faces:
        if ch["die"] == "d6":
            vals = [f.value] + ([9] if f.is_top else [])
            ro = pip_open_radius(ch)
            for val in vals:
                for u, v in pip_points(val):
                    f.obstacles.append(("c", u, v, ro))
        else:
            for t in face_texts(ch, f):
                W, H = number_size(t, plan["p"])
                f.obstacles.append(("b", 0.0, 0.0, W / 2, H / 2))

    # Pecorino basket weave: a staggered lattice of tiny shallow dimples on the rind faces
    if ch.get("basket"):
        s, r = 0.085, 0.024
        for fi, f in enumerate(faces):
            if f.role != "rind" or f.value is None:
                continue
            rows = int(f.rin * 2 / (s * 0.866)) + 4
            for j in range(-rows, rows + 1):
                for i in range(-rows, rows + 1):
                    u = (i + (0.5 if j % 2 else 0.0)) * s
                    v = j * s * 0.866
                    if f.fits(u, v, r) and f.clear_of(u, v, r + 0.01):
                        plan["dimples"].append((fi, u, v, r))

    # marbling / blue veins (painted)
    allowed = [i for i, f in enumerate(faces) if f.allow_extras]
    for _ in range(ch.get("streaks", 0)):
        for _try in range(400):
            fi = rng.choice(allowed)
            f = faces[fi]
            u0 = rng.uniform(-f.rin, f.rin)
            v0 = rng.uniform(-f.rin, f.rin)
            ang = rng.uniform(0, 2 * math.pi)
            length = rng.uniform(0.25, 0.45)
            width = rng.uniform(0.025, 0.04)
            poly, centre = ribbon(rng, u0, v0, ang, length, width)
            if all(f.fits(x, y, 0.005) for x, y in poly) and \
                    all(f.clear_of(x, y, width / 2) for x, y in centre):
                plan["streaks"].append((fi, poly))
                for x, y in centre[::3]:
                    f.obstacles.append(("c", x, y, width / 2))
                break

    # 2-4 extra holes on non-numbered areas
    style = ch["extra"]
    for _ in range(rng.randint(2, 4)):
        for _try in range(400):
            fi = rng.choice(allowed)
            f = faces[fi]
            r = rng.uniform(*ch["extra_r"])
            ro = r * (1.25 if style == "paint" else 1.15 if style == "rough"
                      else math.sqrt(1 - STYLE_OFFSET[style] ** 2))
            xs = [p[0] for p in f.poly2]
            ys = [p[1] for p in f.poly2]
            u = rng.uniform(min(xs), max(xs))
            v = rng.uniform(min(ys), max(ys))
            if f.fits(u, v, ro) and f.clear_of(u, v, ro):
                plan["extras"].append((fi, u, v, r, rng.random()))
                f.obstacles.append(("c", u, v, ro))
                break
    return plan


# ---------------------------------------------------------------- booleans

def boolean_cut(obj, bm_cut, mats, coll):
    if len(bm_cut.faces) == 0:
        bm_cut.free()
        return
    bmesh.ops.recalc_face_normals(bm_cut, faces=list(bm_cut.faces))
    cutter = mesh_object(obj.name + "_Cutter", bm_cut, mats, coll)
    bm_cut.free()
    cutter.hide_render = True
    mod = obj.modifiers.new("Cut", "BOOLEAN")
    mod.operation = "DIFFERENCE"
    mod.solver = "EXACT"
    mod.object = cutter
    mod.use_self = True
    mod.use_hole_tolerant = True
    mod.material_mode = "TRANSFER"
    apply_modifiers(obj)
    me = cutter.data
    bpy.data.objects.remove(cutter)
    bpy.data.meshes.remove(me)
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.dissolve_degenerate(bm, dist=1e-6, edges=list(bm.edges))
    # Note: the pixel 7 has two strokes that meet only at a corner, so each engraved 7 leaves
    # one edge shared by four faces. Harmless for rendering and colliders.
    bm.to_mesh(obj.data)
    bm.free()


def flush_imprints(obj, faces):
    """Painted flecks: they were cut IMPRINT deep; lift their floors back to the surface."""
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    for f in faces:
        d = f.c.dot(f.n)
        for v in bm.verts:
            if abs(v.co.dot(f.n) - d + IMPRINT) < 2e-4:
                rel = v.co - f.c
                if inner_dist((rel.dot(f.right), rel.dot(f.up)), f.poly2) > f.margin * 0.5:
                    v.co += f.n * IMPRINT
    bmesh.ops.remove_doubles(bm, verts=list(bm.verts), dist=1e-5)
    bmesh.ops.dissolve_degenerate(bm, dist=1e-5, edges=list(bm.edges))
    bm.to_mesh(obj.data)
    bm.free()


def build_variant(ch, base, faces, plan, mats, coll, lucky):
    name = f"Die_{ch['name']}_{ch['die']}" + ("_Lucky9" if lucky else "")
    obj = bpy.data.objects.new(name, base.data.copy())
    obj.data.name = name
    coll.objects.link(obj)

    imp = bmesh.new()
    cut = bmesh.new()
    imp_faces = set()

    for fi, f in enumerate(faces):
        if f.value is None:
            continue
        val = 9 if (lucky and f.is_top) else f.value
        if ch["die"] == "d6":
            for k, (u, v) in enumerate(pip_points(val)):
                r = ch["pip_r"]
                prng = seeded(ch["name"], "pip", fi, val, k)
                if ch["pip"] == "paint":
                    add_prism(imp, f, blob(prng, u, v, r), -IMPRINT, 0.03, HOLES)
                    imp_faces.add(fi)
                elif ch["pip"] == "rough":
                    add_rough(cut, f.world(u, v, STYLE_OFFSET["rough"] * r), r, HOLES, prng)
                else:
                    add_sphere(cut, f.world(u, v, STYLE_OFFSET[ch["pip"]] * r), r, HOLES)
        else:
            add_number(cut, f, str(val), plan["p"], HOLES)

    for fi, poly in plan["streaks"]:
        add_prism(imp, faces[fi], poly, -IMPRINT, 0.03, HOLES)
        imp_faces.add(fi)

    style = ch["extra"]
    for k, (fi, u, v, r, s) in enumerate(plan["extras"]):
        f = faces[fi]
        if style == "paint":
            add_prism(imp, f, blob(seeded(ch["name"], "xfleck", k), u, v, r, n=10), -IMPRINT, 0.03, HOLES)
            imp_faces.add(fi)
        elif style == "rough":
            add_rough(cut, f.world(u, v, STYLE_OFFSET["rough"] * r), r, HOLES, seeded(ch["name"], "xpit", k))
        else:
            add_sphere(cut, f.world(u, v, STYLE_OFFSET[style] * r), r, HOLES, segs=16, rings=8)

    for fi, u, v, r in plan["dimples"]:
        add_sphere(cut, faces[fi].world(u, v, 0.6 * r), r, RIND, segs=8, rings=4)

    if len(imp.faces):
        boolean_cut(obj, imp, mats, coll)
        flush_imprints(obj, [faces[i] for i in sorted(imp_faces)])
    else:
        imp.free()
    boolean_cut(obj, cut, mats, coll)
    return obj


# ---------------------------------------------------------------- export

def export_fbx(obj):
    os.makedirs(FBX_DIR, exist_ok=True)
    bpy.context.view_layer.update()
    for o in bpy.context.view_layer.objects:
        if o is not None:
            o.select_set(False)
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    path = os.path.join(FBX_DIR, obj.name + ".fbx")
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, object_types={"MESH"},
        apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL", bake_space_transform=True,
        axis_forward="-Z", axis_up="Y", use_mesh_modifiers=True, mesh_smooth_type="OFF",
        add_leaf_bones=False, bake_anim=False, embed_textures=False, path_mode="AUTO")
    return path


# ---------------------------------------------------------------- preview sheets

def emission_material(name, rgb):
    m = bpy.data.materials.new(name)
    try:
        m.use_nodes = True
    except Exception:
        pass
    nt = m.node_tree
    if nt is not None:
        bsdf = next((n for n in nt.nodes if n.type == "BSDF_PRINCIPLED"), None)
        if bsdf is not None:
            bsdf.inputs["Base Color"].default_value = (0, 0, 0, 1)
            for nm in ("Emission Color", "Emission"):
                if nm in bsdf.inputs:
                    bsdf.inputs[nm].default_value = (*rgb, 1)
                    break
            if "Emission Strength" in bsdf.inputs:
                bsdf.inputs["Emission Strength"].default_value = 1.0
    return m


def build_preview(sets):
    scn = bpy.context.scene
    for eng in ("BLENDER_EEVEE", "BLENDER_EEVEE_NEXT"):
        try:
            scn.render.engine = eng
            break
        except TypeError:
            continue
    try:
        scn.eevee.taa_render_samples = 32
    except Exception:
        pass
    scn.view_settings.view_transform = "Standard"
    try:
        scn.view_settings.look = "None"
    except Exception:
        pass
    scn.render.resolution_x = 1500
    scn.render.resolution_y = 1500
    scn.render.film_transparent = False

    world = bpy.data.worlds.new("PreviewWorld")
    scn.world = world
    bgc = (0.09, 0.085, 0.11)
    world.color = bgc
    try:
        world.use_nodes = True
    except Exception:
        pass
    if world.node_tree is not None:
        bg = next((n for n in world.node_tree.nodes if n.type == "BACKGROUND"), None)
        if bg is not None:
            bg.inputs[0].default_value = (*bgc, 1)
            bg.inputs[1].default_value = 1.0

    rig = bpy.data.collections.new("Preview_Rig")
    scn.collection.children.link(rig)
    S = 2.15
    az, el = math.radians(28), math.radians(32)
    vd = Vector((math.sin(az) * math.cos(el), -math.cos(az) * math.cos(el), math.sin(el)))
    q = vd.to_track_quat("Z", "Y")
    right, up = q @ Vector((1, 0, 0)), q @ Vector((0, 1, 0))

    cam_data = bpy.data.cameras.new("PreviewCam")
    cam_data.type = "ORTHO"
    cam_data.ortho_scale = 3 * S + 0.2
    cam_data.clip_end = 200
    cam = bpy.data.objects.new("PreviewCam", cam_data)
    cam.rotation_mode = "QUATERNION"
    cam.rotation_quaternion = q
    cam.location = vd * 40
    rig.objects.link(cam)
    scn.camera = cam

    def sun(name, toward, strength, angle):
        ld = bpy.data.lights.new(name, "SUN")
        ld.energy = strength
        ld.angle = math.radians(angle)
        lo = bpy.data.objects.new(name, ld)
        lo.rotation_mode = "QUATERNION"
        lo.rotation_quaternion = toward.normalized().to_track_quat("Z", "Y")
        rig.objects.link(lo)

    sun("KeyTopLeft", -right * 0.55 + up * 0.8 + vd * 0.6, 3.2, 8)
    sun("Fill", right * 0.8 + vd * 0.5 - up * 0.2, 0.9, 20)

    label_mat = emission_material("M_PreviewLabel", (0.92, 0.9, 0.95))
    font = None
    try:
        font = bpy.data.fonts.load("<builtin>")
    except Exception:
        pass

    colls = []
    for title, objs in sets:
        coll = bpy.data.collections.new("Preview_" + title)
        scn.collection.children.link(coll)
        colls.append(coll)
        for i, (ch, obj) in enumerate(objs):
            row, col = divmod(i, 3)
            pos = right * ((col - 1) * S) + up * ((1 - row) * S + 0.18)
            inst = obj.copy()
            inst.name = "Preview_" + obj.name
            inst.location = pos
            coll.objects.link(inst)
            cu = bpy.data.curves.new("Label_" + obj.name, "FONT")
            if font is not None:
                cu.font = font
            cu.body = f"{i + 1}. {ch['label']} {ch['die']}" + ("\nLucky 9" if title == "Lucky9" else "")
            cu.align_x = "CENTER"
            cu.size = 0.17
            cu.materials.append(label_mat)
            lab = bpy.data.objects.new("Label_" + obj.name, cu)
            lab.rotation_mode = "QUATERNION"
            lab.rotation_quaternion = q
            lab.location = pos - up * 1.0 + vd * 3.0
            coll.objects.link(lab)
    return colls


def render_previews(colls, names):
    os.makedirs(PREVIEW_DIR, exist_ok=True)
    scn = bpy.context.scene
    for coll, nm in zip(colls, names):
        for c in colls:
            c.hide_render = c is not coll
        scn.render.filepath = os.path.join(PREVIEW_DIR, nm)
        try:
            bpy.ops.render.render(write_still=True)
        except Exception as e:
            print("EEVEE render failed, falling back to Cycles:", e)
            scn.render.engine = "CYCLES"
            scn.cycles.samples = 48
            scn.cycles.device = "CPU"
            bpy.ops.render.render(write_still=True)
        print("rendered", scn.render.filepath)


# ---------------------------------------------------------------- main

def main():
    t0 = time.time()
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scn = bpy.context.scene
    scn.unit_settings.system = "METRIC"
    scn.unit_settings.scale_length = 1.0

    dice_coll = bpy.data.collections.new("Dice")
    scn.collection.children.link(dice_coll)
    work = bpy.data.collections.new("Work")
    scn.collection.children.link(work)

    standard, lucky, exported = [], [], []
    for ch in CHEESES:
        t = time.time()
        mats = [make_material(f"M_{ch['name']}_Main", ch["main"], ch.get("soft", False)),
                make_material(f"M_{ch['name']}_Holes", ch["holes"], ch.get("soft", False))]
        if ch["rind"]:
            mats.append(make_material(f"M_{ch['name']}_Rind", ch["rind"], ch.get("soft", False)))
        base, faces = build_base(ch, mats, work)
        plan = plan_layout(ch, faces)
        for is_lucky in (False, True):
            obj = build_variant(ch, base, faces, plan, mats, dice_coll, is_lucky)
            exported.append(export_fbx(obj))
            (lucky if is_lucky else standard).append((ch, obj))
            print(f"  {obj.name}: {len(obj.data.vertices)} verts, {len(obj.data.polygons)} faces")
        bpy.data.meshes.remove(base.data)
        print(f"{ch['name']} done in {time.time() - t:.1f}s "
              f"(extras {len(plan['extras'])}, streaks {len(plan['streaks'])}, dimples {len(plan['dimples'])})")
    bpy.data.collections.remove(work)

    colls = build_preview([("Standard", standard), ("Lucky9", lucky)])
    dice_coll.hide_render = True
    os.makedirs(os.path.dirname(BLEND_PATH), exist_ok=True)
    bpy.context.preferences.filepaths.save_version = 0      # no CheeseDice.blend1 backups
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH, check_existing=False)
    print("saved", BLEND_PATH)
    print("exported", len(exported), "FBX files to", FBX_DIR)

    if PREVIEW_DIR:
        render_previews(colls, ["cheese_dice_standard.png", "cheese_dice_lucky9.png"])
    print(f"total {time.time() - t0:.1f}s")


main()
