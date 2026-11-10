"""Paints out lettering the picture model put on name plates (it printed a colour code from the
prompt), by median-filtering each plate's face in the prop's colour map. Run after forge_to_props.sh.

    ~/image-to-3dlab/.venv/bin/python tools/clean_plates.py
"""
from pathlib import Path
from PIL import Image, ImageFilter

PROPS = Path(__file__).resolve().parent.parent / "Assets/Resources/Props"
# plate faces in 2048 x 2048 texture pixels: (left, top, right, bottom)
PLATES = {
    "console": [(218, 782, 270, 880), (1644, 862, 1748, 924)],
    "radar": [(1762, 1178, 1848, 1214)],
}

for name, boxes in PLATES.items():
    path = PROPS / f"{name}_base.png"
    im = Image.open(path)
    for box in boxes:
        patch = im.crop(box)
        for _ in range(3):
            patch = patch.filter(ImageFilter.MedianFilter(11))
        im.paste(patch, box[:2])
    im.save(path)
    print("cleaned", name, len(boxes))
