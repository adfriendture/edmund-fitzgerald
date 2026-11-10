#!/bin/zsh
# tools/forge_to_props.sh <asset id> [<asset id> ...]
# Takes a model built by ~/Developer/asset-forge (picture -> Pixal3D -> Finish) and drops it
# into the matching prop slot as Assets/Resources/Props/<id>.fbx with its colour map beside it.
# The game then fits it to the slot's size and pivot (Props.Slots) at start-up.
set -e
cd "$(dirname "$0")/.."
BL=/Applications/Blender.app/Contents/MacOS/Blender
FORGE=~/Developer/asset-forge
LAB=~/image-to-3dlab
for id in "$@"; do
  build="$FORGE/runs/fitz/models/$id/build.json"
  [[ -f "$build" ]] || { echo "no build for $id ($build)"; continue; }
  glb=$(python3 -c "import json;d=json.load(open('$build'));print(d['made'].get('finished') or d['made']['model'])")
  [[ "$glb" = /* ]] || glb="$LAB/$glb"
  [[ -f "$glb" ]] || { echo "missing $glb"; continue; }
  rm -f "Assets/Resources/Props/$id.fbx" "Assets/Resources/Props/${id}_base.png"
  "$BL" -b --python-exit-code 1 -P "$FORGE/scripts/glb2fbx_static.py" -- "$glb" "$PWD/Assets/Resources/Props/$id.fbx" 2>&1 | grep -E "^OK|Error" || true
  ls -la "Assets/Resources/Props/$id.fbx" "Assets/Resources/Props/${id}_base.png" 2>/dev/null
done
