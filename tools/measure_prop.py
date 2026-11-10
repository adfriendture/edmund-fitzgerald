"""Measures a generated model the way the game will fit it, to place the game's own parts on it.

    ~/image-to-3dlab/.venv/bin/python tools/measure_prop.py <model.glb> <slot size m> [--step 0.04] [--yaw Y --pitch P] [--square]

--square finds the yaw and pitch that make the middle of the front flat to the camera (a model built
from a three-quarter picture, seen a little from above, comes in turned and leaning by that view).
The map prints, for rows of height y on the fitted model (base on the floor), the depth of the front
surface at each x in the game's node frame (negative z is toward the player) and a colour letter
from the texture: G dark green, g green, Y brass, k black, w white.
"""
import argparse, numpy as np, trimesh

ap = argparse.ArgumentParser()
ap.add_argument("glb"); ap.add_argument("size", type=float)
ap.add_argument("--step", type=float, default=0.04)
ap.add_argument("--yaw", type=float, default=0); ap.add_argument("--pitch", type=float, default=0)
ap.add_argument("--square", action="store_true")
ap.add_argument("--band", type=float, nargs=2, default=[0.2, 0.6], help="height fractions the squaring looks at")
ap.add_argument("--yaws", type=float, nargs=2, default=[-50, 50], help="yaw range to search (a flat side can otherwise win)")
a = ap.parse_args()
sc = trimesh.load(a.glb)
base = sc.to_geometry() if hasattr(sc, "to_geometry") else sc.dump(concatenate=True)


def turned(yaw, pitch):
    m = base.copy()
    m.apply_transform(trimesh.transformations.rotation_matrix(np.radians(yaw), [0, 1, 0]))
    m.apply_transform(trimesh.transformations.rotation_matrix(np.radians(pitch), [1, 0, 0]))
    return m


def flatness(m):
    lo, hi = m.bounds
    k = a.size / (hi - lo).max()
    ys = np.linspace(lo[1] + (hi[1] - lo[1]) * a.band[0], lo[1] + (hi[1] - lo[1]) * a.band[1], 6)
    xs = np.linspace(lo[0], hi[0], 24)[5:-5]
    o = np.array([[x, y, hi[2] + 1] for y in ys for x in xs])
    loc, _, _ = m.ray.intersects_location(o, np.tile([0, 0, -1], (len(o), 1)), multiple_hits=False)
    return np.std(loc[:, 2]) * k if len(loc) > len(o) // 3 else 9


if a.square:
    best = min(((flatness(turned(y, p)), y, p) for y in range(int(a.yaws[0]), int(a.yaws[1]) + 1, 4) for p in range(-24, 25, 4)))
    _, y0, p0 = best
    best = min(((flatness(turned(y, p)), y, p) for y in np.arange(y0 - 4, y0 + 4.1, 1) for p in np.arange(p0 - 4, p0 + 4.1, 1)))
    print(f"square: yaw {best[1]:+.0f}  pitch {best[2]:+.0f}  (front spread {best[0]*100:.1f} cm)")
    a.yaw, a.pitch = best[1], best[2]

mesh = turned(a.yaw, a.pitch)
lo, hi = mesh.bounds
k = a.size / (hi - lo).max()
cx, cz = (lo[0] + hi[0]) / 2, (lo[2] + hi[2]) / 2
print(f"yaw {a.yaw:+.0f} pitch {a.pitch:+.0f}  fitted size x {(hi[0]-lo[0])*k:.2f} y {(hi[1]-lo[1])*k:.2f} z {(hi[2]-lo[2])*k:.2f} m (scale {k:.3f})")
tex = uv = None
try:
    tex = np.asarray(mesh.visual.material.baseColorTexture.convert("RGB")).astype(float) / 255
    uv = mesh.visual.uv
except Exception:
    pass
ys = np.arange(0, (hi[1] - lo[1]) * k, a.step)
xs = np.arange(-(hi[0] - lo[0]) * k / 2, (hi[0] - lo[0]) * k / 2, a.step)
for y in ys[::-1]:
    o = np.array([[cx + x / k, lo[1] + y / k, hi[2] + 1] for x in xs])
    loc, ray, tri = mesh.ray.intersects_location(o, np.tile([0, 0, -1], (len(o), 1)), multiple_hits=False)
    row = ["   .  "] * len(xs)
    for l, r, t in zip(loc, ray, tri):
        code = " "
        if tex is not None:
            f = mesh.faces[t]
            bc = trimesh.triangles.points_to_barycentric(mesh.triangles[[t]], [l])[0]
            u, v = (uv[f] * bc[:, None]).sum(0)
            c = tex[int((1 - v) % 1 * (tex.shape[0] - 1)), int(u % 1 * (tex.shape[1] - 1))]
            lum = c.mean()
            code = ("G" if c[1] > c[0] * 1.15 and c[1] > c[2] * 1.05 and lum < 0.5 else "g" if c[1] > c[0] * 1.1
                    else "Y" if c[0] > c[2] * 1.4 and lum > 0.35 else "k" if lum < 0.18 else "w" if lum > 0.7 else " ")
        row[r] = f"{-(l[2] - cz) * k:+.2f}{code}"
    print(f"{y:4.2f} " + "".join(row))
print("x:   " + "".join(f"{x:+.2f} " for x in xs))
