"""Builds the pilothouse props in Blender and exports them as FBX into the game's prop slots.

    /Applications/Blender.app/Contents/MacOS/Blender -b -P tools/blender/build_props.py -- wheel telegraph binnacle
    (no names = everything)

Each prop is modelled in metres with the pivot and facing that docs/ASSETS.md asks for.
Materials are named (brass, wood, paint, steel, iron, chrome, black, concrete, cream);
the game maps those names to its own weathered materials, so no textures are exported.
"""
import bpy, bmesh, math, sys, os
from mathutils import Vector, Matrix

_mats = {}
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "Assets", "Resources", "Props")

# ------------------------------------------------------------------ helpers

def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    _mats.clear()   # the old scene's materials are gone with it
    bpy.context.scene.unit_settings.system = 'METRIC'
    bpy.context.scene.unit_settings.scale_length = 1.0


def mat(name):
    if name not in _mats:
        m = bpy.data.materials.new(name)
        m.use_nodes = True
        _mats[name] = m
    return _mats[name]


def finish(obj, material, smooth=True, bevel=0.0):
    obj.data.materials.clear()
    obj.data.materials.append(mat(material))
    if smooth:
        for p in obj.data.polygons:
            p.use_smooth = True
        mod = obj.modifiers.new("EdgeSplit", 'EDGE_SPLIT')
        mod.split_angle = math.radians(40)
    if bevel > 0:
        b = obj.modifiers.new("Bevel", 'BEVEL')
        b.width = bevel
        b.segments = 3
        b.limit_method = 'ANGLE'
    return obj


def cyl(name, material, r, h, at=(0, 0, 0), axis='Z', r2=None, seg=32, bevel=0.0):
    """A cylinder (or truncated cone with r2) centred at `at`, along `axis`."""
    bm = bmesh.new()
    r2 = r if r2 is None else r2
    bmesh.ops.create_cone(bm, cap_ends=True, segments=seg, radius1=r, radius2=r2, depth=h)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    obj = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(obj)
    if axis == 'X': obj.rotation_euler = (0, math.radians(90), 0)
    if axis == 'Y': obj.rotation_euler = (math.radians(90), 0, 0)
    obj.location = at
    return finish(obj, material, bevel=bevel)


def torus(name, material, R, r, at=(0, 0, 0), axis='Z', seg=48, side=12):
    bpy.ops.mesh.primitive_torus_add(major_radius=R, minor_radius=r, major_segments=seg, minor_segments=side, location=at)
    obj = bpy.context.active_object
    obj.name = name
    if axis == 'X': obj.rotation_euler = (0, math.radians(90), 0)
    if axis == 'Y': obj.rotation_euler = (math.radians(90), 0, 0)
    return finish(obj, material)


def box(name, material, size, at=(0, 0, 0), bevel=0.0, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(size=1, location=at)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = size
    obj.rotation_euler = rot
    return finish(obj, material, smooth=False, bevel=bevel)


def sphere(name, material, r, at=(0, 0, 0), scale=(1, 1, 1)):
    bpy.ops.mesh.primitive_uv_sphere_add(radius=r, segments=32, ring_count=16, location=at)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = scale
    return finish(obj, material)


def lathe(name, material, profile, at=(0, 0, 0), axis='Z', seg=48):
    """Spins a (radius, height) profile about the axis. Profile runs bottom to top."""
    bm = bmesh.new()
    verts = [bm.verts.new((r, 0, z)) for r, z in profile]
    edges = [bm.edges.new((verts[i], verts[i + 1])) for i in range(len(verts) - 1)]
    bmesh.ops.spin(bm, geom=verts + edges, cent=(0, 0, 0), axis=(0, 0, 1), angle=math.pi * 2, steps=seg, use_merge=True)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    obj = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(obj)
    if axis == 'X': obj.rotation_euler = (0, math.radians(90), 0)
    if axis == 'Y': obj.rotation_euler = (math.radians(-90), 0, 0)
    obj.location = at
    return finish(obj, material)


def place(o, M):
    """Moves an object by matrix M. A fresh object's matrix_world isn't updated yet, so build it by hand."""
    local = Matrix.Translation(o.location) @ o.rotation_euler.to_matrix().to_4x4() @ Matrix.Diagonal(o.scale).to_4x4()
    o.matrix_world = M @ local


def export(name):
    """Joins everything in the scene into one object named `name` and writes the FBX."""
    objs = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.convert(target='MESH')   # applies the modifiers
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    bpy.ops.object.join()
    # make sure every face points outwards, whichever way a profile was drawn
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.mesh.normals_make_consistent(inside=False)
    bpy.ops.object.mode_set(mode='OBJECT')
    obj = bpy.context.active_object
    obj.name = name
    # parts were placed in world space, so the origin goes to the world origin and the geometry stays put
    bpy.context.scene.cursor.location = (0, 0, 0)
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name + ".fbx")
    # Blender's -Y is the front. The game's loader turns the model a half turn on import so
    # that front ends up facing the player (see Props.Swap).
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL',
                             axis_forward='-Z', axis_up='Y', mesh_smooth_type='FACE', use_mesh_modifiers=True,
                             path_mode='COPY', add_leaf_bones=False, bake_anim=False)
    tris = sum(len(p.vertices) - 2 for p in obj.data.polygons)
    print(f"[props] {name}: {tris} triangles -> {path}")


# ------------------------------------------------------------------ the props
# Blender space here: Z up, -Y is the front (what the player sees).

def wheel():
    """0.6 m six-spoke wheel, in the XZ plane facing -Y, pivot at the hub."""
    reset()
    torus("rim", "wood", 0.30, 0.03, axis='Y', seg=64, side=14)
    torus("rim_band", "brass", 0.30, 0.012, at=(0, -0.028, 0), axis='Y', seg=64, side=8)
    lathe("hub", "brass", [(0, -0.055), (0.07, -0.055), (0.075, -0.045), (0.075, 0.045), (0.07, 0.055), (0, 0.055)], axis='Y')
    lathe("hubcap", "brass", [(0, 0), (0.04, 0), (0.035, 0.02), (0.012, 0.028), (0, 0.03)], at=(0, -0.055, 0), axis='Y')
    for i in range(6):
        a = i * math.pi / 3
        # spoke from the hub to the rim, then the turned handle past it
        s = cyl(f"spoke{i}", "brass", 0.013, 0.30, at=(0, 0, 0.19), axis='Z', seg=16)
        h = lathe(f"handle{i}", "wood", [(0.012, 0.0), (0.02, 0.02), (0.024, 0.05), (0.018, 0.08), (0.025, 0.11), (0.02, 0.15), (0.014, 0.17), (0, 0.175)], at=(0, 0, 0.3), axis='Z')
        for o in (s, h):
            place(o, Matrix.Rotation(a, 4, 'Y'))
    export("wheel")


def telegraph():
    """Brass engine order telegraph, 1.4 m, pivot at the foot, dial on -Y."""
    reset()
    lathe("foot", "brass", [(0, 0), (0.2, 0), (0.2, 0.02), (0.12, 0.06), (0.1, 0.1), (0.1, 0.12), (0, 0.12)])
    lathe("column", "brass", [(0, 0.1), (0.085, 0.1), (0.075, 0.5), (0.085, 0.95), (0.11, 1.0), (0.11, 1.02), (0, 1.02)])
    # the drum head: a short brass cylinder, axis fore-and-aft, dial faces on both ends
    cyl("drum", "brass", 0.24, 0.22, at=(0, 0, 1.18), axis='Y', seg=48, bevel=0.01)
    torus("rim_front", "chrome", 0.225, 0.014, at=(0, -0.115, 1.18), axis='Y')
    torus("rim_back", "chrome", 0.225, 0.014, at=(0, 0.115, 1.18), axis='Y')
    cyl("face_front", "black", 0.21, 0.006, at=(0, -0.118, 1.18), axis='Y')
    cyl("face_back", "black", 0.21, 0.006, at=(0, 0.118, 1.18), axis='Y')
    lathe("bell", "brass", [(0, 0), (0.07, 0), (0.065, 0.03), (0.04, 0.06), (0.02, 0.075), (0, 0.08)], at=(0, 0.02, 1.42))
    cyl("bell_post", "brass", 0.012, 0.06, at=(0, 0.02, 1.4))
    for s in (-1, 1):
        cyl("trunnion", "brass", 0.03, 0.04, at=(s * 0.26, 0, 1.18), axis='X')
    export("telegraph")


def binnacle():
    """Brass compass binnacle with the correcting spheres, 1.45 m, window on -Y."""
    reset()
    lathe("base", "brass", [(0, 0), (0.22, 0), (0.22, 0.03), (0.17, 0.06), (0.15, 0.14), (0.12, 0.2), (0.11, 0.55), (0.12, 0.9), (0.15, 0.97), (0.155, 1.0), (0, 1.0)])
    lathe("head", "brass", [(0, 0.98), (0.19, 0.98), (0.195, 1.0), (0.195, 1.24), (0.19, 1.26), (0.17, 1.3), (0.12, 1.34), (0.06, 1.365), (0, 1.37)])
    lathe("finial", "brass", [(0, 1.36), (0.02, 1.36), (0.02, 1.4), (0.03, 1.41), (0.025, 1.44), (0, 1.45)])
    # the viewing window in the after side: a recessed dark frame
    box("window_frame", "iron", (0.22, 0.02, 0.16), at=(0, -0.19, 1.14), bevel=0.004)
    box("window_glass", "black", (0.19, 0.01, 0.13), at=(0, -0.2, 1.14))
    for s in (-1, 1):
        cyl("arm", "brass", 0.02, 0.2, at=(s * 0.2, 0, 1.1), axis='X')
        lathe("cup", "brass", [(0, 0), (0.07, 0), (0.07, 0.02), (0.05, 0.04), (0, 0.04)], at=(s * 0.3, 0, 1.1), axis='X')
        sphere("ball", "iron", 0.08, at=(s * 0.36, 0, 1.1))
    export("binnacle")


def stool():
    reset()
    lathe("base", "iron", [(0, 0), (0.2, 0), (0.2, 0.015), (0.08, 0.04), (0.035, 0.05), (0.035, 0.72), (0.045, 0.74), (0.045, 0.78), (0, 0.78)])
    lathe("seat", "leather", [(0, 0.78), (0.19, 0.78), (0.2, 0.8), (0.2, 0.84), (0.18, 0.855), (0.1, 0.86), (0, 0.86)])
    for i in range(4):
        a = i * math.pi / 2
        f = cyl(f"foot{i}", "iron", 0.012, 0.18, at=(0, 0, 0.012), axis='X')
        place(f, Matrix.Rotation(a, 4, 'Z') @ Matrix.Translation((0.11, 0, 0)))
    export("stool")


def mug():
    reset()
    lathe("body", "cream", [(0, 0), (0.04, 0), (0.044, 0.005), (0.045, 0.1), (0.047, 0.108), (0.04, 0.11), (0.037, 0.108), (0.037, 0.095), (0, 0.095)])
    cyl("coffee", "coffee", 0.036, 0.004, at=(0, 0, 0.095))
    torus("handle", "cream", 0.03, 0.008, at=(0.056, 0, 0.055), axis='Y', seg=24, side=8)
    export("mug")


def phone():
    """Sound-powered phone in its wall box, pivot at the back face, front on -Y."""
    reset()
    box("box", "paint", (0.26, 0.14, 0.36), at=(0, -0.07, 0), bevel=0.006)
    box("lid_seam", "black", (0.24, 0.002, 0.34), at=(0, -0.141, 0))
    # handset in its cradle
    for z in (0.11, -0.11):
        lathe("cup", "black", [(0, 0), (0.03, 0), (0.035, 0.03), (0.03, 0.045), (0, 0.045)], at=(0, -0.145, z + 0.01), axis='Y')
    cyl("grip", "black", 0.016, 0.2, at=(0, -0.185, 0.01), axis='Z', r2=0.014)
    for z in (0.08, -0.08):
        box("cradle", "paint", (0.08, 0.03, 0.02), at=(0, -0.155, z))
    cyl("crank_shaft", "brass", 0.008, 0.05, at=(0.15, -0.07, -0.05), axis='X')
    cyl("crank_arm", "brass", 0.006, 0.05, at=(0.175, -0.07, -0.03), axis='Z')
    cyl("crank_knob", "wood", 0.01, 0.03, at=(0.175, -0.07, 0.0), axis='Z')
    box("plate", "brass", (0.12, 0.004, 0.05), at=(0, -0.142, -0.13))
    export("phone")


def amradio():
    """Walnut tabletop AM radio, pivot at the centre of the case, dial on -Y."""
    reset()
    box("case", "wood", (0.56, 0.38, 0.26), at=(0, 0.1, 0.0), bevel=0.012)
    box("dial_recess", "black", (0.46, 0.006, 0.13), at=(0, -0.078, 0.03))   # the game's dial glass sits a centimetre proud of it
    box("grille", "cloth", (0.5, 0.006, 0.07), at=(0, -0.09, -0.075))
    for x in (-0.19, -0.11, 0.11, 0.19):
        lathe("knob", "black", [(0, 0), (0.02, 0), (0.022, 0.015), (0.018, 0.025), (0, 0.025)], at=(x, -0.09, -0.075), axis='Y')
    export("amradio")


def vhf():
    """1970s VHF radiotelephone with handset, pivot at the centre of the case, front on -Y."""
    reset()
    box("case", "steel", (0.58, 0.42, 0.28), at=(0, 0.1, 0.0), bevel=0.008)
    box("panel", "black", (0.54, 0.004, 0.24), at=(0, -0.107, 0.0))
    box("window_bezel", "chrome", (0.28, 0.004, 0.12), at=(-0.1, -0.108, 0.03))
    for x in (0.12, 0.22):
        lathe("knob", "chrome", [(0, 0), (0.025, 0), (0.025, 0.02), (0.02, 0.03), (0, 0.03)], at=(x, -0.11, -0.07), axis='Y')
    for x in (-0.22, -0.16, -0.1, -0.04):
        box("button", "black", (0.03, 0.01, 0.015), at=(x, -0.114, -0.07))
    # handset on a hook on the left side
    box("hook", "chrome", (0.02, 0.03, 0.06), at=(-0.3, 0.0, 0.08))
    cyl("grip", "black", 0.016, 0.2, at=(-0.32, 0.0, 0.02), axis='Z')
    for z in (0.12, -0.08):
        lathe("cup", "black", [(0, 0), (0.03, 0), (0.035, 0.03), (0.03, 0.045), (0, 0.045)], at=(-0.32, -0.02, z), axis='Y')
    export("vhf")


def lifering():
    """0.7 m ring in the XZ plane facing -Y, pivot at the centre."""
    reset()
    torus("ring", "orange", 0.28, 0.07, axis='Y', seg=48, side=16)
    for i in range(4):
        a = i * math.pi / 2 + math.pi / 4
        b = box("band", "cream", (0.06, 0.16, 0.16), at=(0, 0, 0))
        place(b, Matrix.Rotation(a, 4, 'Y') @ Matrix.Translation((0.28, 0, 0)))
    export("lifering")


def binoculars():
    reset()
    for s in (-1, 1):
        cyl("barrel", "black", 0.03, 0.14, at=(s * 0.045, 0, 0.035), axis='Y', r2=0.024)
        lathe("objective", "black", [(0.028, 0), (0.034, 0.0), (0.034, 0.02), (0.03, 0.02)], at=(s * 0.045, -0.07, 0.035), axis='Y')
        cyl("eyepiece", "black", 0.014, 0.02, at=(s * 0.045, 0.078, 0.035), axis='Y')
    box("bridge", "black", (0.05, 0.08, 0.03), at=(0, 0, 0.035), bevel=0.004)
    cyl("hinge", "chrome", 0.01, 0.12, at=(0, 0, 0.035), axis='Y')
    export("binoculars")


def radar():
    """Radar console: hammertone cabinet with a hooded scope tilted back, pivot at the base, front on -Y."""
    reset()
    box("cabinet", "steel", (0.62, 0.56, 1.0), at=(0, 0.02, 0.5), bevel=0.01)
    box("base", "black", (0.7, 0.64, 0.06), at=(0, 0.02, 0.03))
    for x in (-0.22, -0.12, -0.02, 0.1):
        lathe("knob", "black", [(0, 0), (0.018, 0), (0.018, 0.02), (0.014, 0.03), (0, 0.03)], at=(x, -0.3, 0.92), axis='Y')
    box("plate", "black", (0.4, 0.004, 0.1), at=(-0.06, -0.302, 0.72))
    # the head: a block tilted back 55°, with the hood ring on its face
    head = box("head", "steel", (0.62, 0.16, 0.62), at=(0, 0, 0), bevel=0.012)
    hood = torus("hood", "black", 0.23, 0.028, at=(0, -0.09, 0), axis='Y', seg=40, side=10)
    visor = box("visor", "black", (0.62, 0.26, 0.1), at=(0, -0.1, 0.26), bevel=0.006, rot=(math.radians(10), 0, 0))
    tilt = Matrix.Translation((0, -0.02, 1.12)) @ Matrix.Rotation(math.radians(-55), 4, 'X')
    for o in (head, hood, visor):
        place(o, tilt)
    export("radar")


ALL = dict(wheel=wheel, telegraph=telegraph, binnacle=binnacle, stool=stool, mug=mug, phone=phone,
           amradio=amradio, vhf=vhf, lifering=lifering, binoculars=binoculars, radar=radar)

if __name__ == "__main__":
    args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    for name in (args or ALL.keys()):
        ALL[name]()
