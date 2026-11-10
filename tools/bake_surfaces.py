"""Bakes the forge's texture pictures into what the game reads at start-up.

    ~/image-to-3dlab/.venv/bin/python tools/bake_surfaces.py

  wall:  luminance detail (rivets, seams, chips), lighting removed, made seamless -> Resources/Surfaces/wall_detail.bytes
  floor: a 2 x 2 block of tiles cut on the grout lines, so it repeats cleanly      -> Resources/Surfaces/floor_tiles.bytes
  photo: the print cropped from the scan                                              -> Resources/Props/family.jpg
The .bytes files are PNGs; the game decodes them itself so it can read the pixels.
"""
from pathlib import Path
import numpy as np
from PIL import Image, ImageFilter

SRC = Path.home() / "Developer/asset-forge/runs/fitz/textures"
OUT = Path(__file__).resolve().parent.parent / "Assets/Resources"
PICK = {"wall": "take1", "floor": "take1", "photo": "take1"}


def seamless(a: np.ndarray, band: float = 0.18) -> np.ndarray:
    """Cross-fades each edge with the opposite one so the picture tiles without a seam."""
    h, w = a.shape[:2]
    out = a.copy()
    for axis, n in ((1, w), (0, h)):
        k = int(n * band)
        t = np.linspace(0, 1, k)[:, None] if axis == 0 else np.linspace(0, 1, k)[None, :]
        if a.ndim == 3: t = t[..., None]
        if axis == 1:
            head, tail = out[:, :k].copy(), out[:, n - k:].copy()
            out[:, :k] = head * (0.5 + 0.5 * t) + tail * (0.5 - 0.5 * t)
            out[:, n - k:] = tail * (1 - 0.5 * t) + head * (0.5 * t)
        else:
            head, tail = out[:k].copy(), out[n - k:].copy()
            out[:k] = head * (0.5 + 0.5 * t) + tail * (0.5 - 0.5 * t)
            out[n - k:] = tail * (1 - 0.5 * t) + head * (0.5 * t)
    return out


def wall():
    im = Image.open(SRC / "wall" / f"{PICK['wall']}.png").convert("L").crop((0, 0, 1024, 860))
    lum = np.asarray(im).astype(np.float32)
    light = np.asarray(im.filter(ImageFilter.GaussianBlur(48))).astype(np.float32)
    detail = lum / np.maximum(light, 1)                  # about 1, rivets and chips either side
    detail = seamless(np.clip(detail, 0.55, 1.35))
    Image.fromarray(np.clip(detail * 160, 0, 255).astype(np.uint8)).resize((768, 645), Image.LANCZOS).save(OUT / "Surfaces/wall_detail.png", "PNG")
    (OUT / "Surfaces/wall_detail.png").rename(OUT / "Surfaces/wall_detail.bytes")


def floor():
    # take1's grout runs at x 3, 342, 683 and y 338, 680, 1020: cut half a grout line in from each
    im = Image.open(SRC / "floor" / f"{PICK['floor']}.png").convert("RGB").crop((3, 338, 683, 1020))
    a = np.asarray(im).astype(np.float32)
    # take the light falloff out so the repeats don't show
    light = np.asarray(Image.fromarray(a.mean(2).astype(np.uint8)).filter(ImageFilter.GaussianBlur(120))).astype(np.float32)
    a = a * (light.mean() / np.maximum(light, 1))[..., None]
    Image.fromarray(np.clip(a, 0, 255).astype(np.uint8)).resize((512, 512), Image.LANCZOS).save(OUT / "Surfaces/floor_tiles.png", "PNG")
    (OUT / "Surfaces/floor_tiles.png").rename(OUT / "Surfaces/floor_tiles.bytes")


def photo():
    im = Image.open(SRC / "photo" / f"{PICK['photo']}.png").convert("RGB").crop((42, 46, 978, 970))
    im.resize((620, 612), Image.LANCZOS).save(OUT / "Props/family.jpg", quality=90)


if __name__ == "__main__":
    wall(); floor(); photo()
    print("baked")
