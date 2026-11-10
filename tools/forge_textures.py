"""Surface pictures for the pilothouse (wall paint, deck covering, ceiling) and the family snapshot,
drawn by the same local Qwen-Image setup asset-forge uses. Not 3D: these become textures.

    ~/image-to-3dlab/.venv/bin/python tools/forge_textures.py [ids...]

Pictures land in ~/Developer/asset-forge/runs/fitz/textures/<id>/takeN.png with a .json receipt.
"""
import hashlib, json, subprocess, sys, time
from pathlib import Path

FORGE = Path.home() / "Developer/asset-forge"
SD_CLI = Path.home() / "image-to-3dlab/vendor/sdcpp/sd-cli"
MODELS = FORGE / "models"
OUT = FORGE / "runs/fitz/textures"

FLAT = ("Seamless tileable texture, a flat orthographic photograph filling the whole frame edge to edge, "
        "evenly and softly lit with no shadows, no perspective, no vignette, no objects. ")
NEG = ("perspective, horizon, room, furniture, objects, shadows, vignette, frame, border, text, lettering, logos, "
       "watermark, people, cartoon, stylised, 3d render")
ITEMS = {
    "wall": (2, FLAT + "A ship's interior steel bulkhead painted mint-green enamel #8FB89E over many earlier coats: "
             "faint brush marks, a few tiny chips showing grey primer, very faint grime, clean and maintained."),
    "floor": (2, FLAT + "Top-down view of a ship's deck covering of worn dark oxblood-brown linoleum tiles, "
              "nine-inch squares with thin seams, scuffed by boots, old wax dulled, a few pale scratches."),
    "ceiling": (1, FLAT + "A ship's ceiling of painted white steel panels, slightly yellowed by years of tobacco "
                "smoke, faint panel seams and rows of small rivet heads, matte."),
    "photo": (3, "A faded 1970s colour snapshot photograph, the whole frame is the photo: a family of four, a mother, "
              "a father in a plaid flannel shirt, a girl and a younger boy, standing together smiling in front of "
              "a white clapboard house in autumn, Midwest, slightly overexposed warm print colours, soft focus, "
              "natural daylight."),
}


def main(ids):
    for id_ in ids or ITEMS:
        takes, prompt = ITEMS[id_]
        folder = OUT / id_
        folder.mkdir(parents=True, exist_ok=True)
        neg = NEG if id_ != "photo" else "text, lettering, logos, watermark, border, cartoon, stylised, 3d render, extra limbs"
        for take in range(1, takes + 1):
            png = folder / f"take{take}.png"
            if png.exists():
                continue
            seed = int(hashlib.sha256(f"{id_}/{take}".encode()).hexdigest()[:8], 16) % 2**31
            started = time.time()
            proc = subprocess.run(
                [str(SD_CLI), "--diffusion-model", str(MODELS / "qwen-image-Q8_0.gguf"),
                 "--vae", str(MODELS / "qwen_image_vae.safetensors"),
                 "--llm", str(MODELS / "qwen_2.5_vl_7b.safetensors"),
                 "-p", prompt, "--negative-prompt", neg, "--cfg-scale", "3.0", "--sampling-method", "euler",
                 "--steps", "25", "--flow-shift", "3", "-W", "1024", "-H", "1024",
                 "--seed", str(seed), "--diffusion-fa", "-o", str(png)], capture_output=True, text=True)
            if proc.returncode or not png.exists():
                print(f"{id_} take {take}: failed\n{proc.stderr[-400:]}", flush=True)
                continue
            png.with_suffix(".json").write_text(json.dumps({"id": id_, "take": take, "seed": seed, "prompt": prompt,
                "negative": neg, "model": "Qwen-Image Q8_0 (Apache-2.0)", "seconds": round(time.time() - started)}, indent=2))
            print(f"{id_} take {take}: {round(time.time() - started)} s", flush=True)


if __name__ == "__main__":
    main(sys.argv[1:])
